#define SERIAL_DEBUG 1
#include <Arduino.h>
#include "MICS6814.h"
#include "RF24.h"
#include "pin-api.h"
#define CE 9
#define CSN 10
void printf_begin(void);


enum {
  PACKAGE_TEMPERATURE,
  PACKAGE_BATTERY,
  PACKAGE_CO2
};

RF24 radio(CE, CSN);
const uint8_t num_channels = 128;
uint8_t values[num_channels];
const int num_reps = 100;

#define PIN_CO2 5
#define PIN_NH3 6
#define PIN_NO3 7

MICS6814 misc(PIN_CO2, PIN_NO3, PIN_NH3);

struct Payload {
  int pkgID;
  float data;
};

void setup() {
  pinMode(A1, OUTPUT);
  Serial.begin(4800);
  printf_begin();
  radio.begin();
  radio.setAutoAck(false);
  radio.setDataRate(RF24_250KBPS);
  radio.setPALevel(RF24_PA_LOW);
  radio.setCRCLength(RF24_CRC_16);
  radio.setPayloadSize(sizeof(Payload));
  radio.startListening();
  radio.printDetails();
  delay(5000);
  radio.stopListening();
  int i = 0;
  while ( i < num_channels )  {
    printf("%x", i >> 4);
    ++i;
  }
  printf("\a");
  i = 0;
  while ( i < num_channels ) {
    printf("%x", i & 0xf);
    ++i;
  }
  printf("\a");

  radio.setChannel(0x30);
  radio.openWritingPipe(0x01F3F5F78FLL);

  misc.calibrate(A3);
}

void loop(void)
{
  radio.powerUp();
  digitalWrite(A1, 1);
  Payload a;
  a.pkgID = PACKAGE_TEMPERATURE;
  a.data = 24;
  if (radio.write(&a, sizeof(Payload)))
    Serial.println("Success transmitting payload.");
  else
    Serial.println("Failed transmitting payload.");
  digitalWrite(A1, 0);
  radio.powerDown();
  delay(5000);

  Serial.println("CO2: " + String(misc.measure(CO)) + ", NH3:" + String(misc.measure(NH3)) + ", N02: " + String(misc.measure(NO2)));
}
int serial_putc( char c, FILE * ) {
  Serial.write( c );
  return c;
}

void printf_begin(void) {
  fdevopen( &serial_putc, 0 );
}



/*

void setup()
{
  pinMode(A1, OUTPUT);
  pinMode(A2, OUTPUT);
  pinMode(A3, OUTPUT);
}
void loop(void)
{
  digitalWrite(A1, 1);   // turn the LED on (HIGH is the voltage level)
  digitalWrite(A2, 1);
  digitalWrite(A3, 1);
  delay(50);                       // wait for a second
  analogWrite(A1, LOW);    // turn the LED off by making the voltage LOW
  delay(50);                       // wait for a second
  analogWrite(A2, LOW);
  delay(50);                       // wait for a second
  analogWrite(A3, LOW);
  delay(50);
}
*/