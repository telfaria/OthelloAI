
# Architecture

## Overview

The system separates game rules, AI algorithms and execution.

Othello.Console
    |
    v
Othello.AI
    |
    v
Othello.Core



## Core

Responsible for:

- Board
- Stone
- Move
- Rules
- Game state


## AI

Responsible for decision making.

Examples:
RandomAI
GreedyAI
MinimaxAI
MctsAI


All implement:
IOthelloAI

## Future
WinUI application will reference:
- Core
- AI

No dependency from Core to UI.

