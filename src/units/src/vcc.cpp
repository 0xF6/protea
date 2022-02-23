#include "vcc.h"
vcc::vcc( const float correction )
  : m_correction(correction) { }

#if defined(__AVR_ATmega32U4__) || defined(__AVR_ATmega1280__) || defined(__AVR_ATmega2560__)
#define ADMUX_VCCWRT1V1 (_BV(REFS0) | _BV(MUX4) | _BV(MUX3) | _BV(MUX2) | _BV(MUX1))
#elif defined (__AVR_ATtiny24__) || defined(__AVR_ATtiny44__) || defined(__AVR_ATtiny84__)
#define ADMUX_VCCWRT1V1 (_BV(MUX5) | _BV(MUX0))
#elif defined (__AVR_ATtiny25__) || defined(__AVR_ATtiny45__) || defined(__AVR_ATtiny85__)
#define ADMUX_VCCWRT1V1 (_BV(MUX3) | _BV(MUX2))
#else
#define ADMUX_VCCWRT1V1 (_BV(REFS0) | _BV(MUX3) | _BV(MUX2) | _BV(MUX1))
#endif

float vcc::read_volts(void)
{
  if (ADMUX != ADMUX_VCCWRT1V1)
  {
    ADMUX = ADMUX_VCCWRT1V1;
    delayMicroseconds(350);
  }
  ADCSRA |= _BV(ADSC);
  while (bit_is_set(ADCSRA,ADSC));
  float vcc = 1.1 * 1024.0 / ADC;
  vcc *= m_correction;

  return vcc;
}

float vcc::read_perc(const float range_min, const float range_max, const boolean clip)
{
  float perc = 100.0 * (read_volts()-range_min) / (range_max-range_min);
  if (clip)
    perc = constrain(perc, 0.0, 100.0);

  return perc;
}