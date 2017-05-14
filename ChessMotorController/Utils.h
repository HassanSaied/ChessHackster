#pragma once
#include <Arduino.h>

// Constants
#define BLOCK_SIZE   40
#define MAX_PER_BYTE 248

// Signals
#define SIG_BOM      249
#define SIG_EOM      250
#define SIG_LOAD     251
#define SIG_SAVE     252
#define SIG_VALIDATE 253
#define SIG_CONFIRM  254
#define SIG_CANCEL   255

// Simple parser from byte array given by master
void parseInput(byte, byte, byte, byte, int&, int&, int*, bool&);

// Save current state
void saveState(int, int, int*);

// Load a previously saved state (source and moves) and calculate (destination)
void loadState(int&, int&, int&, int&, int*);



