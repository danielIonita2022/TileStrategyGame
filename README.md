# Tile Strategy Game (Carcassonne)
Tile Strategy is a Unity-based adaptation of the classic tile-placement game Carcassonne for mobile platforms. This multiplayer game allows players to build a medieval landscape, place meeples strategically, and compete for the highest score.

## Features:

### 1. Multiplayer Support

- Host or join a game lobby via Unity Relay and Lobby Services.
- Synchronize game data seamlessly between all clients using Unity Netcode.

### 2. Tile Placement System

- Dynamically place tiles on a growing board.
- Highlight valid placement positions and check for adjacency rules.

### 3. Meeple Placement

- Place meeples on features such as cities, roads, or monasteries.
- Automatically validate and restrict meeple placements based on game rules.
- Interactive UI for meeple selection and placement.

### 4. Real-Time Score Updates

- Track player scores dynamically as features are completed.
- Separate scoring for roads, cities, and monasteries.

### 5. Interactive UI

- Player-specific HUD displaying scores, meeple counts, and turn indicators.
- Main menu with hosting and joining options.
- End game screen summarizing player scores.

## How to Play

### Hosting a Game

- Click "Host" in the main menu.
- Share the lobby code with friends.
- Start the game once all players join.

### Joining a Game

- Enter the lobby code provided by the host.
- Click "Join" to enter the lobby.
- Wait for the host to start the game.

### Gameplay

- Each turn, a player draws a tile and places it on the board.
- After placing a tile, the player can place a meeple on a feature (if valid).
- Points are awarded for completed features.
- The game ends when all tiles are placed or no valid moves remain.


## Game Screenshots

![Main Menu](https://github.com/danielIonita2022/TileStrategyGame/blob/main/Screenshots/main_menu.jpeg)

![Gameplay 1](https://github.com/danielIonita2022/TileStrategyGame/blob/main/Screenshots/gameplay1.jpeg)

![Gameplay 2](https://github.com/danielIonita2022/TileStrategyGame/blob/main/Screenshots/gameplay2.jpeg)
