#include "MICS6814.h"

MICS6814::MICS6814(int pinCO, int pinNO2, int pinNH3)
{
	_pinCO  = pinCO;
	_pinNO2 = pinNO2;
	_pinNH3 = pinNH3;
}

#define PTIME 30
void pulse(int pin, int times) {
  do {
    digitalWrite(pin, HIGH);
    delay(PTIME);
    digitalWrite(pin, LOW);
    delay(PTIME);
  }
  while (times--);
}

void MICS6814::calibrate(int pinPulse)
{
	uint8_t seconds = 10;

	uint8_t delta = 20;

	uint16_t bufferNH3[seconds];
	uint16_t bufferCO[seconds];
	uint16_t bufferNO2[seconds];

	uint8_t pntrNH3 = 0;
	uint8_t pntrCO = 0;
	uint8_t pntrNO2 = 0;

	uint16_t fltSumNH3 = 0;
	uint16_t fltSumCO = 0;
	uint16_t fltSumNO2 = 0;

	uint16_t curNH3;
	uint16_t curCO;
	uint16_t curNO2;

	bool isStableNH3 = false;
	bool isStableCO  = false;
	bool isStableNO2 = false;

	for (int i = 0; i < seconds; ++i)
	{
		bufferNH3[i] = 0;
		bufferCO[i]  = 0;
		bufferNO2[i] = 0;
	}

	do
	{
		delay(1000);

    pulse(pinPulse, 10);

		unsigned long rs = 0;

		delay(50);
		for (int i = 0; i < 3; i++)
		{
			delay(1);
			rs += analogRead(_pinNH3);
		}

		curNH3 = rs / 3;
		rs = 0;
    pulse(pinPulse, 10);
		delay(50);
		for (int i = 0; i < 3; i++)
		{
			delay(1);
			rs += analogRead(_pinCO);
		}

		curCO = rs / 3;
		rs = 0;
    pulse(pinPulse, 10);
		delay(50);
		for (int i = 0; i < 3; i++)
		{
			delay(1);
			rs += analogRead(_pinNO2);
		}

		curNO2 = rs / 3;

		fltSumNH3 = fltSumNH3 + curNH3 - bufferNH3[pntrNH3];
		fltSumCO  = fltSumCO  + curCO  - bufferCO[pntrCO];
		fltSumNO2 = fltSumNO2 + curNO2 - bufferNO2[pntrNO2];

		bufferNH3[pntrNH3] = curNH3;
		bufferCO[pntrCO]   = curCO;
		bufferNO2[pntrNO2] = curNO2;

		isStableNH3 = abs(fltSumNH3 / seconds - curNH3) < delta;
		isStableCO  = abs(fltSumCO  / seconds - curCO)  < delta;
		isStableNO2 = abs(fltSumNO2 / seconds - curNO2) < delta;

		pntrNH3 = (pntrNH3 + 1) % seconds;
		pntrCO  = (pntrCO  + 1) % seconds;
		pntrNO2 = (pntrNO2 + 1) % seconds;
    pulse(pinPulse, 10);
    Serial.print("NH3: " + String(abs(fltSumNH3 / seconds - curNH3)) + ", CO2: "
    + String(abs(fltSumCO / seconds - curCO)) + ", NO2: "
    + String(abs(fltSumNO2 / seconds - curNO2)) + "\n");
	} while (!isStableNH3 || !isStableCO || !isStableNO2);

	_baseNH3 = fltSumNH3 / seconds;
	_baseCO  = fltSumCO  / seconds;
	_baseNO2 = fltSumNO2 / seconds;
}

void MICS6814::loadCalibrationData(
	uint16_t baseNH3,
	uint16_t baseCO,
	uint16_t baseNO2)
{
	_baseNH3 = baseNH3;
	_baseCO  = baseCO;
	_baseNO2 = baseNO2;
}
float MICS6814::measure(gas_t gas)
{
	float ratio;
	float c = 0;

	switch (gas)
	{
	case CO:
		ratio = getCurrentRatio(CH_CO);
		c = pow(ratio, -1.179) * 4.385;
		break;
	case NO2:
		ratio = getCurrentRatio(CH_NO2);
		c = pow(ratio, 1.007) / 6.855;
		break;
	case NH3:
		ratio = getCurrentRatio(CH_NH3);
		c = pow(ratio, -1.67) / 1.47;
		break;
	}

	return isnan(c) ? -1 : c;
}
uint16_t MICS6814::getResistance(channel_t channel) const
{
	unsigned long rs = 0;
	int counter = 0;

	switch (channel)
	{
	case CH_CO:
		for (int i = 0; i < 100; i++)
		{
			rs += analogRead(_pinCO);
			counter++;
			delay(2);
		}
	case CH_NO2:
		for (int i = 0; i < 100; i++)
		{
			rs += analogRead(_pinNO2);
			counter++;
			delay(2);
		}
	case CH_NH3:
		for (int i = 0; i < 100; i++)
		{
			rs += analogRead(_pinNH3);
			counter++;
			delay(2);
		}
	}

	return counter != 0 ? rs / counter : 0;
}

uint16_t MICS6814::getBaseResistance(channel_t channel) const
{
	switch (channel)
	{
	case CH_NH3:
		return _baseNH3;
	case CH_CO:
		return _baseCO;
	case CH_NO2:
		return _baseNO2;
	}

	return 0;
}

float MICS6814::getCurrentRatio(channel_t channel) const
{
	float baseResistance = (float)getBaseResistance(channel);
	float resistance = (float)getResistance(channel);

	return resistance / baseResistance * (1023.0 - baseResistance) / (1023.0 - resistance);

	return -1.0;
}