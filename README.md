# Speech to Text Assistant

## Overview
The Speech to Text Assistant is a Windows desktop application that allows users to convert spoken words into text. The application detects when an input field is focused in any Windows application and displays an overlay with functionality to initiate speech recognition.

## Features
- Detects focused input fields in other applications.
- Provides an overlay with speech recognition controls next to the input field.
- Converts spoken words into text and inputs them into the focused field.
- User-friendly interface for seamless interaction.

## Project Structure
```
SpeechToTextAssistant
├── src
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── Services
│   │   ├── InputDetectionService.cs
│   │   ├── SpeechRecognitionService.cs
│   │   └── OverlayService.cs
│   ├── Models
│   │   └── SpeechRecognitionResult.cs
│   ├── ViewModels
│   │   └── MainViewModel.cs
│   └── Helpers
│       ├── Win32Interop.cs
│       └── UIAutomationHelper.cs
├── Properties
│   └── AssemblyInfo.cs
├── Resources
│   └── Icons.xaml
├── SpeechToTextAssistant.csproj
├── App.config
└── README.md
```

## Installation
1. Clone the repository:
   ```
   git clone https://github.com/yourusername/SpeechToTextAssistant.git
   ```
2. Open the solution in your preferred IDE.
3. Restore the NuGet packages if necessary.
4. Build the project.

## Usage
1. Run the application.
2. Focus on any input field in a Windows application.
3. Click the speech recognition button in the overlay to start converting speech to text.
4. Speak clearly, and the recognized text will be input into the focused field.

## Contributing
Contributions are welcome! Please feel free to submit a pull request or open an issue for any suggestions or improvements.

## License
This project is licensed under the MIT License. See the LICENSE file for details.