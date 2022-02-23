#ifndef VCC_H
#define VCC_H

#include "Arduino.h"

class vcc
{
  public:
    vcc( const float correction = 1.0 );

    float read_volts(void);

    float read_perc(const float range_min = 0.0, const float range_max = 0.0, const boolean clip = true);

  protected:
    float m_correction;
};

#endif