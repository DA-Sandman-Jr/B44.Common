# GAME_TITLE — Development Guidelines

<!-- Bootstrap for repository-local guidance only. After this repository opts
     into B44.Standards agent synchronization, the build inserts and maintains
     organization/game sections directly below this title. Do not copy shared
     doctrine into the local sections below. -->

## What This Is

Describe the game, supported platforms, engine/.NET versions, and current
release status in a few durable sentences.

## Repository-Specific Hard Rules

- Add only invariants unique to this game or repository.
- Put subsystem-specific rules in nested `CLAUDE.md` files near that code.

## Commands

Document the exact restore, build, test, and game-launch commands that work in
this repository.

## Architecture

Map the engine-free Core project, Godot presentation project, tests, and the
few directories an agent must understand before making a change. Keep volatile
feature inventories in dedicated design documents and link them here.

## Tests

Document the normal test command plus any repository-specific discovery or
environment requirements.
