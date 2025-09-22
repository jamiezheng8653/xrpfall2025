//Header file for reading and processing user keyboard strokes
//use is for ease of testing code without constantly needing a Xbox controller
#pragma once
#include <windows.h>
#pragma comment(lib, "User32.lib")

class Keyboard {
    public:
        Keyboard(); //constructor
        ~Keyboard(); //destructor
        Keyboard(const Keyboard &obj); //copy constructor
        Keyboard& operator=(const Keyboard& other); //copy assignment operator
        char ReadStroke();
    
};