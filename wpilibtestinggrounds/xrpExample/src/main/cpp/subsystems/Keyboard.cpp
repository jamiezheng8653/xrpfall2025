#include "subsystems/Keyboard.h"

/// @brief Only returns what key was pressed if it's WASD
Keyboard::Keyboard()
{
}

//destructor
Keyboard::~Keyboard()
{
    //if the class is storing anything, be sure to delete and null it
}

//copy constructor
Keyboard::Keyboard(const Keyboard &obj)
{
    //if the class is storing anything, ensure obj's matches this'
}

//copy assignment operator
Keyboard &Keyboard::operator=(const Keyboard &other)
{
    if (this == &other) return *this; //are we return something equal to itself
    
    //if the keyboard class held any kind of data, 
    //make sure to copy over the data to other
    
    return *this;
}

/// @brief Check for if WASD was pressed
/// @return Cooresponding character of WASD key pressed. 0 otherwise
char Keyboard::ReadStroke()
{
    char input = ' ';
    //0x8000 code for if the passed in keycode is being pressed down
    //list of virtual key codes:
    //https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
    //W
    if(GetKeyState(0x57) & 0x8000){
        return 'W';
    }
    //A
    if(GetKeyState(0x41) & 0x8000){
        return 'A';
    }
    //S
    if(GetKeyState(0x53) & 0x8000){
        return 'S';
    }
    //D
    if(GetKeyState(0x44) & 0x8000){
        return 'D';
    }
    //Q
    if(GetKeyState(0x51) & 0x8000){
        return 'Q';
    }
    //E
    if(GetKeyState(0x45) * 0x8000){
        return 'E';
    }
    //R
    if(GetKeyState(0x52) & 0x8000){
        return 'R';
    }
    return 0;
}
