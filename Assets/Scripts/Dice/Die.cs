﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Animations;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace Dice
{
    public enum DesignAndColor : byte
    {
        Unknown = 0,
        Generic,
        V3_Orange,
        V4_BlackClear,
        V4_WhiteClear,
        V5_Grey,
        V5_White,
        V5_Black,
        V5_Gold,
        Onyx_Back,
        Hematite_Grey,
        Midnight_Galaxy,
        Aurora_Sky
    }

    public partial class Die
        : MonoBehaviour
    {
        public enum RollState : byte
        {
            Unknown = 0,
            OnFace,
            Handling,
            Rolling,
            Crooked
        };

        public enum ConnectionState
        {
            Invalid = -1,   // This is the value right after creation
            Available,      // This is a die we knew about and scanned
            Connecting,     // This die is in the process of being connected to
            Identifying,    // Getting info from the die, making sure it is valid to be used (right firmware, etc...)
            Ready,          // Die is ready for general use
            Disconnecting,  // We are currently disconnecting from this die
        }

        ConnectionState _connectionState = ConnectionState.Invalid; // Use property to change value
        public ConnectionState connectionState
        {
            get => _connectionState;
            private set
            {
                if (value != _connectionState)
                {
                    Debug.Log($"Die connection state change: {_connectionState} => {value}");
                    _connectionState = value;
                }
            }
        }

        public enum LastError
        {
            None = 0,
            ConnectionError,
            Disconnected
        }

        public LastError lastError { get; private set; } = LastError.None;

        /// <summary>
        /// This data structure mirrors the data in firmware/bluetooth/bluetooth_stack.cpp
        /// </sumary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CustomAdvertisingData
        {
            // Die type identification
            public DesignAndColor designAndColor; // Physical look, also only 8 bits
            public byte faceCount; // Which kind of dice this is

            // Device ID
            public uint deviceId;

            // Current state
            public Die.RollState rollState; // Indicates whether the dice is being shaken
            public byte currentFace; // Which face is currently up
            public byte batteryLevel; // 0 -> 255
        };

        public int faceCount { get; private set; } = 0;
        public DesignAndColor designAndColor { get; private set; } = DesignAndColor.Unknown;
        public uint deviceId { get; private set; } = 0;
        public string firmwareVersionId { get; private set; } = "Unknown";
        public string address { get; private set; } = ""; // name is stored on the gameObject itself
        public uint dataSetHash { get; private set; } = 0;
        public uint flashSize { get; private set; } = 0;

        public RollState state { get; private set; } = RollState.Unknown;
        public int face { get; private set; } = -1;

        public float? batteryLevel { get; private set; } = null;
        public bool? charging { get; private set; } = null;
        public int? rssi { get; private set; } = null;

        public delegate void TelemetryEvent(Die die, AccelFrame frame);
        public TelemetryEvent _TelemetryReceived;
        public event TelemetryEvent TelemetryReceived
        {
            add
            {
                if (_TelemetryReceived == null)
                {
                    // The first time around, we make sure to request telemetry from the die
                    RequestTelemetry(true);
                }
                _TelemetryReceived += value;
            }
            remove
            {
                _TelemetryReceived -= value;
                if (_TelemetryReceived == null || _TelemetryReceived.GetInvocationList().Length == 0)
                {
                    if (connectionState == ConnectionState.Ready)
                    {
                        // Unregister from the die telemetry
                        RequestTelemetry(false);
                    }
                    // Otherwise we can't send bluetooth packets to the die, can we?
                }
            }
        }

        public delegate void StateChangedEvent(Die die, RollState newState, int newFace);
        public StateChangedEvent OnStateChanged;

        public delegate void ConnectionStateChangedEvent(Die die, ConnectionState oldState, ConnectionState newState);
        public ConnectionStateChangedEvent OnConnectionStateChanged;

        public delegate void ErrorEvent(Die die, LastError error);
        public ErrorEvent OnError;

        public delegate void SettingsChangedEvent(Die die);
        public SettingsChangedEvent OnSettingsChanged;

        public delegate void AppearanceChangedEvent(Die die, int newFaceCount, DesignAndColor newDesign);
        public AppearanceChangedEvent OnAppearanceChanged;

        public delegate void BatteryLevelChangedEvent(Die die, float? level, bool? charging);
        public BatteryLevelChangedEvent OnBatteryLevelChanged;

        public delegate void RssiChangedEvent(Die die1, int? rssi);
        public RssiChangedEvent OnRssiChanged;

        // Lock so that only one 'operation' can happen at a time on a die
        // Note: lock is not a real multithreaded lock!
        bool bluetoothOperationInProgress = false;

        // Internal delegate per message type
        delegate void MessageReceivedDelegate(IDieMessage msg);
        Dictionary<DieMessageType, MessageReceivedDelegate> messageDelegates;

        void Awake()
        {
            messageDelegates = new Dictionary<DieMessageType, MessageReceivedDelegate>();

            // Setup delegates for face and telemetry
            messageDelegates.Add(DieMessageType.State, OnStateMessage);
            messageDelegates.Add(DieMessageType.Telemetry, OnTelemetryMessage);
            messageDelegates.Add(DieMessageType.DebugLog, OnDebugLogMessage);
            messageDelegates.Add(DieMessageType.NotifyUser, OnNotifyUserMessage);
            messageDelegates.Add(DieMessageType.PlaySound, OnPlayAudioClip);
        }

        public void Setup(
            string name,
            string address,
            uint deviceId,
            int faceCount,
            DesignAndColor design,
            out System.Action<ConnectionState> outConnectionSetter,
            out System.Action<LastError> outLastErrorSetter)
        {
            bool appearanceChanged = faceCount != this.faceCount || design != this.designAndColor;
            this.name = name;
            this.address = address;
            this.deviceId = deviceId;
            this.faceCount = faceCount;
            this.designAndColor = design;
            if (appearanceChanged)
            {
                OnAppearanceChanged?.Invoke(this, faceCount, designAndColor);
            }
            outConnectionSetter = SetConnectionState;
            outLastErrorSetter = SetLastError;
        }

        public void UpdateAddress(string address)
        {
            this.address = address;
        }

        public void UpdateAdvertisingData(int rssi, CustomAdvertisingData newData)
        {
            bool appearanceChanged = faceCount != newData.faceCount || designAndColor != newData.designAndColor;
            bool rollStateChanged = state != newData.rollState || face != newData.currentFace;
            faceCount = newData.faceCount;
            designAndColor = newData.designAndColor;
            deviceId = newData.deviceId;
            state = newData.rollState;
            face = newData.currentFace;
            batteryLevel = (float)newData.batteryLevel / 255.0f;
            this.rssi = rssi;

            // Trigger callbacks
            OnBatteryLevelChanged?.Invoke(this, batteryLevel, charging);
            if (appearanceChanged)
            {
                OnAppearanceChanged?.Invoke(this, faceCount, designAndColor);
            }
            if (rollStateChanged)
            {
                OnStateChanged?.Invoke(this, state, face);
            }
            OnRssiChanged?.Invoke(this, rssi);
        }

        void SetConnectionState(ConnectionState newState)
        {
            if (newState != connectionState)
            {
                var oldState = connectionState;
                connectionState = newState;
                OnConnectionStateChanged?.Invoke(this, oldState, newState);
            }
        }

        void SetLastError(LastError newError)
        {
            lastError = newError;
            OnError?.Invoke(this, newError);
        }

        public void UpdateInfo(System.Action<Die, bool> onInfoUpdatedCallback)
        {
            if (connectionState == ConnectionState.Identifying)
            {
                StartCoroutine(UpdateInfoCr(onInfoUpdatedCallback));
            }
            else
            {
                onInfoUpdatedCallback?.Invoke(this, false);
            }
        }

        IEnumerator UpdateInfoCr(System.Action<Die, bool> onInfoUpdatedCallback)
        {
            // Ask the die who it is!
            yield return GetDieInfo(null);

            // Ping the die so we know its initial state
            yield return Ping();

            onInfoUpdatedCallback?.Invoke(this, true);
        }

        public void OnData(byte[] data)
        {
            // Process the message coming from the actual die!
            var message = DieMessages.FromByteArray(data);
            if (message != null)
            {
                Debug.Log("Got message of type " + message.GetType());

                if (messageDelegates.TryGetValue(message.type, out MessageReceivedDelegate del))
                {
                    del.Invoke(message);
                }
            }
        }

    }
}