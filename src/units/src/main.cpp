#include <Arduino.h>
#include "MICS6814.h"
#include "RF24.h"
#include "pin-api.h"
#define CE 48
#define CSN 49
void printf_begin(void);

RF24 radio(CE, CSN);
const uint8_t num_channels = 128;
uint8_t values[num_channels];
const int num_reps = 100;
void setup() {
  Serial.begin(9600);
  printf_begin();
  radio.begin();
  radio.setAutoAck(false);
  radio.startListening();

  radio.printDetails();
  delay(5000);

  radio.stopListening();
  int i = 0;
  while ( i < num_channels )  {
    printf("%x", i >> 4);
    ++i;
  }
  printf("\n\r");
  i = 0;
  while ( i < num_channels ) {
    printf("%x", i & 0xf);
    ++i;
  }
  printf("\n\r");
}

void loop(void)
{
  memset(values, 0, sizeof(values));
  int rep_counter = num_reps;
  while (rep_counter--) {
    int i = num_channels;
    while (i--) {
      radio.setChannel(i);
      radio.startListening();
      delayMicroseconds(128);
      radio.stopListening();
      if ( radio.testCarrier() )
        ++values[i];
    }
  }
  int i = 0;
  while ( i < num_channels ) {
    printf("%x", min(0xf, values[i] & 0xf));
    ++i;
  }
  printf("\n\r");
}
int serial_putc( char c, FILE * ) {
  Serial.write( c );
  return c;
}

void printf_begin(void) {
  fdevopen( &serial_putc, 0 );
}