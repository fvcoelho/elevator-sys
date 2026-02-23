Single Elevator System Specification (Easy Level)
Problem Overview

Design a single-elevator system handling passenger requests efficiently for floors 1-10.

Core Requirements

Implement an Elevator class (current floor, state, target floors).

Implement an ElevatorController class (manages the elevator).

Handle pickup and destination requests.

Ensure thread safety for concurrent operations.

Use a FIFO scheduling algorithm.

Difficulty Level

Easy Level: Single Elevator System

Key Classes
Elevator Class

currentFloor: int

state: ElevatorState (IDLE, MOVING_UP, MOVING_DOWN, DOOR_OPEN)

targetFloors: list of int

Methods: moveUp(), moveDown(), openDoor(), closeDoor(), addRequest(floor)

ElevatorController Class

elevator: Elevator

Methods: requestElevator(floor, direction), processRequests()