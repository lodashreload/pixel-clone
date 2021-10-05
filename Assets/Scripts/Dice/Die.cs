﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

using Central = Systemic.Pixels.Unity.BluetoothLE.Central;
using Peripheral = Systemic.Pixels.Unity.BluetoothLE.ScannedPeripheral;

namespace Dice
{
    public enum DieDesignAndColor : byte
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

    public enum DieRollState : byte
    {
        Unknown = 0,
        OnFace,
        Handling,
        Rolling,
        Crooked
    };

    public enum DieConnectionState
    {
        Invalid = -1,   // This is the value right after creation
        Available,      // This is a die we knew about and scanned
        Connecting,     // This die is in the process of being connected to
        Identifying,    // Getting info from the die, making sure it is valid to be used (right firmware, etc...)
        Ready,          // Die is ready for general use
        Disconnecting,  // We are currently disconnecting from this die
    }

    public enum DieLastError
    {
        None = 0,
        ConnectionError,
        Disconnected
    }

    public abstract partial class Die
        : MonoBehaviour
    {
        DieConnectionState _connectionState = DieConnectionState.Invalid; // Use property to change value
        public DieConnectionState connectionState
        {
            get => _connectionState;
            protected set
            {
                Debug.Assert(System.Threading.Thread.CurrentThread.ManagedThreadId == 1);
                if (value != _connectionState)
                {
                    Debug.Log($"Die {name} connection state change: {_connectionState} => {value}");
                    var oldState = _connectionState;
                    _connectionState = value;
                    OnConnectionStateChanged?.Invoke(this, oldState, value);
                }
            }
        }

        public bool isConnectingOrReady => (_connectionState == DieConnectionState.Connecting)
                || (_connectionState == DieConnectionState.Identifying)
                || (_connectionState == DieConnectionState.Ready);

        public DieLastError lastError { get; protected set; } = DieLastError.None;

        // name is stored on the gameObject itself
        public int faceCount { get; protected set; } = 0;
        public DieDesignAndColor designAndColor { get; protected set; } = DieDesignAndColor.Unknown;
        public uint deviceId { get; protected set; } = 0;
        public string firmwareVersionId { get; protected set; } = "Unknown";
        public uint dataSetHash { get; protected set; } = 0;
        public uint flashSize { get; protected set; } = 0;

        public DieRollState state { get; protected set; } = DieRollState.Unknown;
        public int face { get; protected set; } = -1;

        public float? batteryLevel { get; protected set; } = null;
        public bool? charging { get; protected set; } = null;
        public int? rssi { get; protected set; } = null;

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
                    if (connectionState == DieConnectionState.Ready)
                    {
                        // Unregister from the die telemetry
                        RequestTelemetry(false);
                    }
                    // Otherwise we can't send bluetooth packets to the die, can we?
                }
            }
        }

        public delegate void StateChangedEvent(Die die, DieRollState newState, int newFace);
        public StateChangedEvent OnStateChanged;

        public delegate void ConnectionStateChangedEvent(Die die, DieConnectionState oldState, DieConnectionState newState);
        public ConnectionStateChangedEvent OnConnectionStateChanged;

        public delegate void ErrorEvent(Die die, DieLastError error);
        public ErrorEvent OnError;

        public delegate void AppearanceChangedEvent(Die die, int newFaceCount, DieDesignAndColor newDesign);
        public AppearanceChangedEvent OnAppearanceChanged;

        public delegate void BatteryLevelChangedEvent(Die die, float? level, bool? charging);
        public BatteryLevelChangedEvent OnBatteryLevelChanged;

        public delegate void RssiChangedEvent(Die die1, int? rssi);
        public RssiChangedEvent OnRssiChanged;

        // Internal delegate per message type
        protected delegate void MessageReceivedDelegate(IDieMessage msg);
        protected Dictionary<DieMessageType, MessageReceivedDelegate> messageDelegates;

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

        protected abstract void WriteData(byte[] bytes, System.Action<Die, bool, string> onWriteResult);
    }
}