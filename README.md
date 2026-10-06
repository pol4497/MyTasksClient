# MyTasks Client

A .NET MAUI task manager for Windows and Android. It is the desktop and mobile client for the [MyTasks API](https://github.com/pol4497/MyTasks) and follows the look and features of the MyTasks web client.

## Features

- View your tasks
- Add a task with a title, category, due date and description
- Search, filter by status, category and due date, sort, and page through results
- Edit a task in place: double-click (Windows) or double-tap (phone) a field
- Delete a task with the **×** in the card's corner

## Requirements

- Windows 10 or 11
- [Visual Studio 2026](https://visualstudio.microsoft.com/) with the **.NET Multi-platform App UI development** workload (it installs the .NET 10 SDK and the Android SDK)
- The MyTasks API running locally (see its repository for setup)
- For Android: an emulator or a phone, see [Running on Android](#running-on-android)

Tested on Windows and Android. The project also contains iOS and Mac Catalyst targets, which have not been tested.

## Getting started

1. Clone the repository:
   ```
   git clone https://github.com/pol4497/MyTasksClient.git
   ```
   or download, extract and open `MyTasksClient.slnx` in Visual Studio
2. Start the API with its **http** profile. The app expects it at `http://localhost:5235`.
3. Pick a target in the toolbar, **Windows Machine** or an Android device or emulator, and press **F5**.

## Running on Android

Either option works. Both are covered in Microsoft's [device setup guide](https://dotnet.microsoft.com/learn/maui/first-app-tutorial/device-setup).

- **Emulator.** Create a virtual device in the Android Device Manager. Nothing else is needed: the app reaches the API through the emulator's `10.0.2.2` address automatically.
- **Your own phone.** Turn on USB debugging and connect it with a cable. A phone cannot see your PC's `localhost`, so forward the API port to it:

  ```
  adb reverse tcp:5235 tcp:5235
  ```

  Run it from **Tools → Android → Android Adb Command Prompt** in Visual Studio. Run it again whenever the cable is reconnected or adb restarts.

## Configuration

The API address used by Debug builds is in `MyTasksClient/Infrastructure/ApiConfiguration.cs` (`DevelopmentPort`, 5235 by default). Change it if your API listens elsewhere. If you change the host, also allow it in `MyTasksClient/Platforms/Android/Resources/xml/network_security_config.xml`, because Android blocks plain HTTP by default.

Release builds fail until the production address is set in the same file.
