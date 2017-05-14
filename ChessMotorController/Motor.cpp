#include "Motor.h"

Motor::Motor(int steps, int dir_pin, int step_pin, int ms1_pin, int ms2_pin, int ms3_pin, int calibrationPin, Direction calibrationDirection) : A4988(steps, dir_pin, step_pin, ms1_pin, ms2_pin, ms3_pin), calibrationPin(calibrationPin), calibrationDirection(calibrationDirection)
{
	pinMode(calibrationPin, INPUT);
	digitalWrite(calibrationPin, HIGH);
}

void Motor::moveOneMillimeter(Direction directionFlag)
{
	int cycle = FULL_CYCLE;
	if (directionFlag)
		cycle *= -1;
	this->rotate(cycle);
}

void Motor::calibrate()
{
	int cycle = FULL_CYCLE;
	if (calibrationDirection)
		cycle *= -1;
	while (digitalRead(calibrationPin) == HIGH)
		this->rotate(cycle);
}