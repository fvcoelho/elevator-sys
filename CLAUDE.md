# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an elevator control system project. The codebase will implement elevator scheduling, control logic, and potentially a user interface for managing elevator operations.

## Development Setup

*Note: This section will be populated once the project structure is established.*

### Common Commands

```bash
# Build the project
# [To be added based on chosen build system]

# Run tests
# [To be added based on chosen test framework]

# Run linter
# [To be added based on chosen linter]

# Start development server
# [To be added based on chosen framework]
```

## Architecture

*This section will be updated as the architecture evolves.*

### Core Components

- **Elevator Control Logic**: Handles elevator movement, scheduling algorithms (e.g., SCAN, LOOK, nearest-first)
- **Request Queue Management**: Manages and prioritizes elevator requests from different floors
- **State Management**: Tracks elevator positions, directions, and door states
- **UI Layer**: Provides interface for users to interact with the elevator system (if applicable)

### Key Considerations

- **Concurrency**: Elevator systems require careful handling of concurrent requests
- **Safety**: Door operations, weight limits, and emergency protocols must be prioritized
- **Scheduling Algorithms**: Choose efficient algorithms to minimize wait times
- **Real-time Updates**: System state should be reflected accurately in real-time

## Coding Standards

- Use TypeScript/shadcn components for any .tsx files (per global instructions)
- Implement proper error handling for all elevator control operations
- Ensure thread-safety for concurrent elevator operations
- Add appropriate logging for state transitions and critical operations
