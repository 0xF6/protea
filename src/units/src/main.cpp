#define SERIAL_DEBUG
#define RFNANO 1
#define HUMIDITY 1



#include <Arduino.h>
//#include "MICS6814.h"
#include "RF24.h"
#include "pin-api.h"
#include <GyverPower.h>
#include "printf.h"
#define CE 7
#define CSN 8


#define RF_POWER A5
#define LED_AWAIT A1
#define LED_RELAY A2
#ifdef RFNANO
#define LED_STATUS LED_BUILTIN
#elif
#define LED_STATUS A3
#endif

#ifdef HUMIDITY

#define DHTTYPE DHT21
#include <OneWire.h>
#include <Wire.h>
#include "DHT.h"

#define DHTPIN 7

DHT dht(DHTPIN, DHTTYPE);

#endif




void pulse(int pin, int times);

enum {
  PACKAGE_TEMPERATURE,
  PACKAGE_BATTERY,
  PACKAGE_CO2
};

RF24 radio(9, 10);
const uint8_t num_channels = 128;
uint8_t values[num_channels];
const int num_reps = 100;

#define PIN_CO2 5
#define PIN_NH3 6
#define PIN_NO3 7

//MICS6814 misc(PIN_CO2, PIN_NO3, PIN_NH3);

struct Payload {
  int pkgID;
  float data;
};

bool usePowerManagement = false;


void signal_await() {

  static volatile bool flag = false;

  flag = !flag;

  digitalWrite(LED_AWAIT, flag ? HIGH : LOW);
}
void signal_status() {
  static volatile bool flag_status = false;

  flag_status = !flag_status;

  digitalWrite(LED_STATUS, flag_status ? HIGH : LOW);
}


void signal_error() {
  for (int i = 0; i != 100; i++)
  {
    digitalWrite(LED_STATUS, HIGH);
    delay(200);
    digitalWrite(LED_STATUS, LOW);
    delay(200);
  }
}

void setup_power()
{
  if (!usePowerManagement)
    return;
  //signal_await();
  //power.autoCalibrate();
  //power.setSleepMode(STANDBY_SLEEP);
  //power.bodInSleep(true);
  //signal_await();
}

void setupRf()
{
  signal_await();

  if (!radio.begin()) {
    Serial.println(F("radio hardware is not responding!!"));
    signal_error();
    //while (1) {} // hold in infinite loop
  }
  radio.setAutoAck(false);
  //radio.setDataRate(RF24_1MBPS);
  //radio.setPALevel(RF24_PA_HIGH);
  radio.setPayloadSize(sizeof(Payload));
  #ifdef RFNANO
  //radio.printDetails();
  #endif

  radio.setChannel(0x10);
  radio.openWritingPipe(0x01F3F5F78FLL);
  signal_await();
}


void record_metrics() {
  signal_await();
  #ifdef HUMIDITY
  delay(3000);
  //dht.begin();

  float hum = dht.readHumidity();

  float temp = dht.readTemperature();

  Serial.print(hum);
  Serial.print(", ");
  Serial.println(temp);

  if (isnan(temp) || isnan(hum))
    return;
  #endif

  signal_await();
}

void powerSleep()
{
  radio.powerDown();
  delay(50);
  digitalWrite(RF_POWER, LOW); // disable 5v -> 3.3v
  if (usePowerManagement)
    power.sleepDelay(20 * 1000);
  else
    delay(2 * 1000);
  digitalWrite(RF_POWER, HIGH); // enable 5v -> 3.3v
  delay(50);
  radio.powerUp();
}
OneWire ds();


void setup() {
  #ifdef RFNANO
  Serial.begin(9600);
  printf_begin();
  #endif
  //pinMode(LED_AWAIT, OUTPUT);
  //pinMode(LED_RELAY, OUTPUT);
  pinMode(LED_STATUS, OUTPUT);

  setup_power();
  //setupRf();

  dht.begin();

  Serial.println("humidity, temperature");
}
void loop(void)
{
  record_metrics();
  /*Payload a;
  a.pkgID = PACKAGE_TEMPERATURE;
  a.data = 24;
  if (radio.write(&a, sizeof(Payload)))
  {
    Serial.println("Success transmitting payload.");
    pulse(A2, 10);
  }
  else
    Serial.println("Failed transmitting payload.");
  digitalWrite(A1, 0);
  delay(5000);*/

  //power_sleep();
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