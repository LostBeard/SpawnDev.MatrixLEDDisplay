# SpawnDev.MatrixLEDDisplay
Blazor WebAssembly code and demo app for communicating with the "Matrix LED Display" by Merkury Innovations. 

![Matrix LED Display](https://raw.githubusercontent.com/LostBeard/SpawnDev.MatrixLEDDisplay/master/SpawnDev.MatrixLEDDisplay.Demo/wwwroot/mi-matrix-display-400x334.png)

This neat $20 USB powered display features a 16x16 multi-color LED grid
that can be found at places like Walmart [here](https://www.walmart.com/ip/Merkury-Innovations-Bluetooth-Matrix-LED-Pixel-Display/5150283693).
It communicates with an 
Android phone app [MatrixPanel Plus](https://play.google.com/store/apps/details?id=com.wzjledaxc.ledplus) app,
or an iPhone app [Matrix Panel Plus](https://apps.apple.com/us/app/matrix-panel-plus/id6743264417)
using Bluetooth Low Energy (BLE.)

There is a bit of a problem with this display, as one YouTube [reviewer](https://www.youtube.com/watch?v=QN0TxJoeTNk) found out... BLE does not work on every phone, preventing the app from communicating with the display.

So if you bought one of these cool displays and want an alternative to the default app, this library and demo app app can help you do that.

## References
- 99% Awesome protocol work: [offe/mi-led-display](https://github.com/offe/mi-led-display)

## WIP
Created 2025-08-17. Current version allows connecting to the display and drawing a spread of colors.

### Todo
- Add library methods for common tasks.
- Live drawing canvas
- Animated images
- Simple image upload
- Generative AI images
