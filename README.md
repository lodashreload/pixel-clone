# PixelsApp

Mobile (iOS & Android, phones only) and Windows app that connects to the Pixels dice.
Please note that the Windows app is a rendering the UI as it would on a phone, it's not optimized for desktop screens.

With this app you may create and preview LED's patterns, activate profiles and manage presets.

Find our online guide [here](https://github.com/GameWithPixels/PixelsApp/wiki/Pixels-App-Guide).

## Patterns

Lighting patterns let you customize how a dice will light up.
There's a wide spectrum of possiblities, from a simple flash of colors to a rainbow effect, or more advanced effects such as waterfall, rotating rings, etc.

## Profiles

Profiles define when and how your dice will light up. A profile is made up of a list of rules.
Each rule associates an event (for example *roll is equal to 1*, or *dice is picked up*) with an action
such as playing a luminous pattern on the die or an audio clip in the app.

## Presets

A preset associate any number of dice with specific profiles to activate.
It's quick way to switch all or a subset of your dice to different profiles at once.

# External ressources

## Unity packages

### Paid packages

To build this app, you will need to purchase several Unity packages:

* [Bluetooth LE for iOS, tvOS and Android](https://assetstore.unity.com/packages/tools/network/bluetooth-le-for-ios-tvos-and-android-26661)
* [Loading Icons](https://assetstore.unity.com/packages/2d/gui/loading-icons-89411)
* [InfiniWheel for uGUI](https://assetstore.unity.com/packages/tools/gui/infiniwheel-for-ugui-unity-4-6-28660)

### Free packages

The app is using a few MIT licensed packages, Unity should download them automatically:

* [Newtonsoft.Json for Unity](https://github.com/jilleJr/Newtonsoft.Json-for-Unity)
* [Unity Native File Picker Plugin](https://github.com/yasirkula/UnityNativeFilePicker)
* [Unity Native Gallery Plugin](https://github.com/yasirkula/UnityNativeGallery)
* [Unity Simple File Browser](https://github.com/yasirkula/UnitySimpleFileBrowser)

If you're having issues with the Unity Package Manager, you can download packages using [Open UPM](https://openupm.com/).

```
npm install -g openupm-cli
```

```
openupm add jillejr.newtonsoft.json-for-unity 
openupm add com.yasirkula.nativefilepicker
openupm add com.yasirkula.nativegallery
openupm add com.yasirkula.simplefilebrowser
```

### ScreenshotCompanion

[ScreenshotCompanion](https://assetstore.unity.com/packages/tools/utilities/screenshot-companion-67779) is a free asset and is directly included in our repository. GitHub page [here](https://github.com/Pfannkuchen/ScreenshotCompanion).

## Font

The app comes with the very nice [Louis George Café](https://www.dafont.com/louis-george-caf.font) free font.

## Icons

Free [Basic UI Icons](https://www.flaticon.com/packs/basic-ui-5?word=basic) - Icons made by [Pixel perfect](https://www.flaticon.com/authors/pixel-perfect)

## Sound files

* [Explosion](https://freesound.org/people/Omar%20Alvarado/sounds/93741/)
* [Failure](https://freesound.org/people/FunWithSound/sounds/394900/)
* [Fanfare Trumpets](https://freesound.org/people/FunWithSound/sounds/456966/)
* [Fireball](https://freesound.org/people/Julien%20Matthey/sounds/105016/)
* [Magic Missiles](https://freesound.org/people/spookymodem/sounds/249817/)
* [Sad Trombone](https://freesound.org/people/kirbydx/sounds/175409/)
* [Success](https://freesound.org/people/grunz/sounds/109662/)

# Build

## iOS (iPhone)

You'll probably need to change the bundle identifier, sign the device with your own certificates
and remove manual provisionning profile setup by Unity in Build Settings.

If you get a `MapFileParser.sh: Permission denied` error, check this [link](https://issuetracker.unity3d.com/issues/ios-mapfileparser-dot-sh-permission-denied-when-building-xcode-project-built-from-windows-directly-to-a-macos-shared-folder).

Also don't forget to tell iOS to trust your app certificate in `General/Profiles & Device Management/Apple Development` on your iPhone.

## Android (phones)

Nothing specific.
