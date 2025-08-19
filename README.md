# SpawnDev.MatrixLEDDisplay
Blazor WebAssembly code and demo app for communicating with the "Matrix LED Display" by Merkury Innovations, aka "MI Matrix Display". 

[Demo Web App](https://lostbeard.github.io/SpawnDev.MatrixLEDDisplay/)

![Matrix LED Display](https://raw.githubusercontent.com/LostBeard/SpawnDev.MatrixLEDDisplay/master/SpawnDev.MatrixLEDDisplay.Demo/wwwroot/mi-matrix-display-400x334.png)

This neat 160mm (6.3 inch) square USB powered display features a 16x16 multi-color LED grid
and can be found at places like Walmart [here](https://www.walmart.com/ip/Merkury-Innovations-Bluetooth-Matrix-LED-Pixel-Display/5150283693) for about $20. 

## The problem
The instructions that come with this display tells the user to install an app on their phone named "MI Matrix Display"... 
but there is no app with that name in the iOS App Store or the Android Play Store. There are apps that claim to be the new version of the official app, 
[MatrixPanel Plus](https://play.google.com/store/apps/details?id=com.wzjledaxc.ledplus) on Android,
and [Matrix Panel Plus](https://apps.apple.com/us/app/matrix-panel-plus/id6743264417) on iOS,
 but the publisher is a one-off publisher named "Chrisamy" not the display maker "Merkury Innovations".
 Merkury Innovations website doesn't list this display. 
 This YouTube [reviewer](https://www.youtube.com/watch?v=QN0TxJoeTNk) had the same issue.

I tried "MatrixPanel Plus" on 2 Android phones and the app didn't see or connect to the display on either one. The problem appears to be the app itself because my old Moto E running Android 10 was able to connect to and control the MI Matrix Display using Google Chrome and this demo.
The app must work on some devices though, as the GitHub repo I found with a lot of Bluetooth protocol work for this display apparently used that app. Reference: [offe/mi-led-display/snoops/README.md](https://github.com/offe/mi-led-display/blob/main/snoops/README.md)

So if you bought one of these cool displays and want an alternative to the default app, this library and demo app can help you do that.

## References
- Awesome protocol work: [offe/mi-led-display](https://github.com/offe/mi-led-display)

## WIP
Created 2025-08-17. Current version allows connecting to the display and drawing a spread of colors.

## Nerd stuff
From Chrome Bluetooth Devices  
- chrome://bluetooth-internals/#devices  

Device Name: MI Matrix Display   
Services:  
- 0000ffd0-0000-1000-8000-00805f9b34fb - Primary  
- Characteristics:  
  - 0000ffd1-0000-1000-8000-00805f9b34fb - WriteWithoutResponse  
  - 0000ffd2-0000-1000-8000-00805f9b34fb - Notify  
    - Descriptors:  
      - 00002902-0000-1000-8000-00805f9b34fb  
- 0000af30-0000-1000-8000-00805f9b34fb 

### Todo
- Add library methods for common tasks.
- Live drawing canvas
- Animated images
- Generative AI images
