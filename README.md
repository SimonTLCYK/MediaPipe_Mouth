# MediaPipe_Mouth
This project provides an interactive game that trains players' mouth muscle to form different mouth gesture.

## Game Rules
Player is required to change the mouth shape to match the corresponding ball when a ball reaches the line. 

Balls appeared in random intervals representing different mouth shapes at the middle of the screen, moving towards the line. 

There are different mouth shapes: A(red), I(blue), U(green). 

Players need to reach a threshold score representing how likely the shape matches to be recognised as a specific shape. 

Matching shape increases one point, game ends after a number of balls appear. 

 
## Installation
1. Unity Hub and Unity Editor version 2022.3.34f1 should be installed.
2. [Download](https://github.com/SimonTLCYK/MediaPipe_Mouth/archive/refs/heads/main.zip) whole project and unzip.
3. Naviagate in Unity Hub: 'Add' -> 'Add project from disk'.
4. Select the extracted project folder.

## Remarks
This project depends on MediaPipe Unity Plugin (version: 0.14.4). 

More information on how `Mouth.cs` utilize the data gathered from MediaPipe Facemesh to compute mouth shape score, please refer to documentation in [Assets/Scripts/Mouth.cs](https://github.com/SimonTLCYK/MediaPipe_Mouth/blob/main/Assets/Scripts/Mouth.cs) and description in [Mouth_cs_description.txt](https://github.com/SimonTLCYK/MediaPipe_Mouth/blob/main/Mouth_cs_description.txt).
