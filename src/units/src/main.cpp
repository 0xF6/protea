#define SERIAL_DEBUG
#define HUMIDITY 1



#include <Arduino.h>
//#include "MICS6814.h"
#include "RF24.h"
#include "pin-api.h"
#include <GyverPower.h>

#define CE 9
#define CSN 10


#define RF_POWER A5
#define LED_AWAIT A1
#define LED_RELAY A2
#define LED_STATUS A3


#ifdef HUMIDITY

#define DHTTYPE DHT21
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

RF24 radio(CE, CSN);
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

  flag = !flag;

  digitalWrite(LED_STATUS, flag ? HIGH : LOW);
}


signal_error() {
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
  signal_await();
  if (!usePowerManagement)
    return;
  power.autoCalibrate();
  power.setSleepMode(STANDBY_SLEEP);
  power.bodInSleep(true);
  signal_await();
}

void setupRf()
{
  signal_await();
  radio.begin();
  radio.setAutoAck(false);
  radio.setDataRate(RF24_250KBPS);
  radio.setPALevel(RF24_PA_HIGH);
  radio.setPayloadSize(sizeof(Payload));

  radio.setChannel(0x10);
  radio.openWritingPipe(0x01F3F5F78FLL);
  signal_await();
}


void record_metrics() {
  signal_await();
  #ifdef HUMIDITY
  delay(7000);
  dht.begin();

  float hum = dht.readHumidity();

  float temp = dht.readTemperature();

  if (isnan(temp) || isnan(hum))
    return;
  #endif

  signal_await();
}

void power_sleep()
{
  radio.powerDown();
  delay(50);
  digitalWrite(RF_POWER, LOW); // enable 5v -> 3.3v
  if (usePowerManagement)
    power.sleepDelay(20 * 1000);
  else
    delay(20 * 1000);
  digitalWrite(RF_POWER, HIGH); // disable 5v -> 3.3v
  delay(50);
  radio.powerUp();
}


void setup() {
  pinMode(LED_AWAIT, OUTPUT);
  pinMode(LED_RELAY, OUTPUT);
  pinMode(LED_STATUS, OUTPUT);

  setup_power();
  setupRf();

  pinMode(A1, OUTPUT);
  pinMode(A2, OUTPUT);
}

void loop(void)
{
  Payload a;
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
  delay(5000);

  power_sleep();
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