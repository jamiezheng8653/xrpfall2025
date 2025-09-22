// Copyright (c) FIRST and other WPILib contributors.
// Open Source Software; you can modify and/or share it under the terms of
// the WPILib BSD license file in the root directory of this project.

#include "Robot.h"
#include "subsystems/Keyboard.h"
#include <math.h>

#include <frc2/command/CommandScheduler.h>

//only used to read WASD inputs
Keyboard kb; 

//references to motors
//frc::XRPMotor lMotor{0}; //left
//frc::XRPMotor rMotor{1}; //right
frc::PWMSparkMax lMotor{0};
frc::PWMSparkMax rMotor{1};

frc::DifferentialDrive m_robotDrive {
  [&](double output) {lMotor.Set(output);},
  [&](double output) {rMotor.Set(output);}};

//frc::XboxController m_controller{0};
//frc::Joystick m_controller {0};
frc::Timer m_timer;

//quick math constants for degree to radian conversion
//multiply with desired angle in degrees
const double degToRad = 3.1415/180; 


Robot::Robot() {
  //technically out the box both motors run opposite directions. 
  //need to invert so the wheels can run the "same" direction 
  rMotor.SetInverted(true); 
  m_robotDrive.SetExpiration(100_ms);
  m_timer.Start();
}

/**
 * This function is called every 20 ms, no matter the mode. Use
 * this for items like diagnostics that you want to run during disabled,
 * autonomous, teleoperated and test.
 *
 * <p> This runs after the mode specific periodic functions, but before
 * LiveWindow and SmartDashboard integrated updating.
 */
void Robot::RobotPeriodic() {
  frc2::CommandScheduler::GetInstance().Run();
}

/**
 * This function is called once each time the robot enters Disabled mode. You
 * can use it to reset any subsystem information you want to clear when the
 * robot is disabled.
 */
void Robot::DisabledInit() {}

void Robot::DisabledPeriodic() {}

/**
 * This autonomous runs the autonomous command selected by your {@link
 * RobotContainer} class.
 */
void Robot::AutonomousInit() {
  m_autonomousCommand = m_container.GetAutonomousCommand();

  if (m_autonomousCommand != nullptr) {
    m_autonomousCommand->Schedule();
  }
}

void Robot::AutonomousPeriodic() {}

void Robot::TeleopInit() {
  // This makes sure that the autonomous stops running when
  // teleop starts running. If you want the autonomous to
  // continue until interrupted by another command, remove
  // this line or comment it out.
  if (m_autonomousCommand != nullptr) {
    m_autonomousCommand->Cancel();
    m_autonomousCommand = nullptr;
  }
  m_timer.Reset();

}

/**
 * This function is called periodically during operator control.
 */
void Robot::TeleopPeriodic() {
  //check if any WASD key is being pressed and return which one
  char readStroke = kb.ReadStroke();

  //depending on which WASD key, adjust motor/steering behavior accordingly
  switch(readStroke){
    case 'W': //maps to Axis[1]; moves forwards
      //motors go in positive direction
      lMotor.Set(0.1);
      rMotor.Set(0.1);
      break;

    //case 'A': //maps to Axis[0], does not move
    //case 'Q': //literally doesn't do anything, can't turn with Q/E Steering
    case 'E':
      //turn left
      lMotor.Set(0.0);
      rMotor.Set(0.1);
      break;

    case 'S': //maps to Axis[1]; moves backwards
      //motors go in negative direction
      lMotor.Set(-0.1);
      rMotor.Set(-0.1);
      break; 

    //case 'D': //maps to Axis[0], does not move 
    //case 'E': //maps to Axis[2], rotates around Z axis
    case 'R':
      //turn right
      lMotor.Set(0.1);
      rMotor.Set(0.0);
      break; 

    case '0': //only resets Axis[1]
      lMotor.Set(0.0);
      rMotor.Set(0.0);
      break;
  }
}

/**
 * This function is called periodically during test mode.
 */
void Robot::TestPeriodic() {}

#ifndef RUNNING_FRC_TESTS
int main() {
  return frc::StartRobot<Robot>();
}
#endif
