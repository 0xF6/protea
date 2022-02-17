#define SERVER
#include <Arduino.h>
#include "RF24.h"
#include "printf.h"

#ifdef CLIENT
#define LED_STATUS A1
#else
#define LED_STATUS LED_BUILTIN
#endif
RF24 radio(9, 10, 10000000U / 10);

enum
{
  PACKAGE_TEMPERATURE,
  PACKAGE_BATTERY,
  PACKAGE_CO2,
  PACKAGE_HUMIDITY
};

struct Payload
{
  byte hash_0;
  int pkgID;
  float data;
  byte hash_1;
};
#define PAYLOAD_HASH_0 12
#define PAYLOAD_HASH_1 56

void signal_error(int pin = -1, int count = 10)
{
  if (pin == -1)
    pin = LED_STATUS;
  for (int i = 0; i != count; i++)
  {
    digitalWrite(pin, HIGH);
    delay(200);
    digitalWrite(pin, LOW);
    delay(200);
  }
}

const uint8_t num_channels = 126;
uint8_t values[num_channels];
const int num_reps = 100;
bool constCarrierMode = 0;

#ifdef SERVER

void setupRf()
{
  if (!radio.begin())
  {
    Serial.println(F("radio hardware is not responding!!"));

    while (1)
    {
      signal_error();
    } // hold in infinite loop
  }
  radio.setCRCLength(RF24_CRC_DISABLED);
  radio.disableCRC();
  radio.disableAckPayload();
  radio.setAutoAck(false);
  radio.setDataRate(RF24_250KBPS);
  radio.setPALevel(RF24_PA_MIN);
  radio.setPayloadSize(sizeof(Payload));

  radio.setChannel(0);
  radio.openReadingPipe(0, 0xB3B4B5B60FLL);
  radio.openReadingPipe(1, 0xB4B4B5B60FLL);
  radio.openReadingPipe(2, 0xB5B4B5B60FLL);
  radio.openReadingPipe(3, 0xB6B4B5B60FLL);
  radio.startListening();
  radio.printDetails();
}

void setup()
{
  Serial.begin(9600);
  printf_begin();
  // pinMode(LED_AWAIT, OUTPUT);
  // pinMode(LED_RELAY, OUTPUT);
  pinMode(LED_STATUS, OUTPUT);

  setupRf();
}
void error()
{
  Serial.println("No data");
  digitalWrite(LED_STATUS, HIGH);
  delay(1200);
  digitalWrite(LED_STATUS, LOW);
  delay(1200);
}
void loop(void)
{
  uint8_t pipe;
  if (!radio.available(&pipe))
  {
    delay(10);
    // error();
    return;
  }
  else
  {
    Payload a;
    radio.read(&a, sizeof(Payload));

    Serial.print(F("Received "));
    Serial.print(pipe);
    Serial.print(F(": "));
    switch (a.pkgID)
    {
      case PACKAGE_TEMPERATURE:
        Serial.print(F("TEMPERATURE"));
        break;
      case PACKAGE_HUMIDITY:
        Serial.print(F("HUMIDITY"));
        break;
      default:
        break;
    }

    Serial.print(F(", "));
    Serial.println(a.data);
    delay(700);
  }

  /*
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
  delay(5000);*/

  // power_sleep();
}
#endif
#ifdef CLIENT
#define SERIAL_DEBUG
//#define RFNANO 1
#define HUMIDITY 1

//#include "MICS6814.h"
#include <GyverPower.h>

#define RF_POWER A5
#define LED_AWAIT A2
#define LED_RELAY A3

#ifdef HUMIDITY

#define DHTTYPE DHT21
#include <OneWire.h>
#include <Wire.h>
#include "DHT.h"

#define DHTPIN 7

DHT dht(DHTPIN, DHTTYPE);

#endif

#define PIN_CO2 5
#define PIN_NH3 6
#define PIN_NO3 7

// MICS6814 misc(PIN_CO2, PIN_NO3, PIN_NH3);

bool usePowerManagement = true;
static bool flag_await = false;
static bool flag_status = false;
void signal_await()
{
  flag_await = !flag_await;

  digitalWrite(LED_AWAIT, flag_await ? HIGH : LOW);
}
void signal_status()
{
  flag_status = !flag_status;

  digitalWrite(LED_STATUS, flag_status ? HIGH : LOW);
}

void setup_power()
{
  if (!usePowerManagement)
    return;
  signal_await();
  Serial.println("power1");
  power.autoCalibrate();
  power.setSleepMode(POWERDOWN_SLEEP);
  power.bodInSleep(true);
  signal_await();
}

void setupRf()
{
  delay(1500);
  if (!radio.begin())
  {
    Serial.println(F("radio hardware is not responding!!"));

    while (1)
    {
      signal_error(LED_STATUS, 3);
      // signal_error(LED_RELAY, 1);
      // signal_error(LED_AWAIT, 3);
      Serial.println(F("radio hardware is not responding!!"));
      if (radio.begin())
        break;
    } // hold in infinite loop
  }
  radio.disableAckPayload();
  radio.setAutoAck(false);
  radio.setCRCLength(RF24_CRC_DISABLED);
  radio.disableCRC();
  radio.setDataRate(RF24_250KBPS);
  radio.setPALevel(RF24_PA_MIN);
  radio.setPayloadSize(sizeof(Payload));

  radio.setChannel(0);
  radio.openWritingPipe(0xB3B4B5B60FLL);
  Serial.println("rf ok");
}

void record_metrics(float *hum, float *temp)
{
  digitalWrite(LED_AWAIT, HIGH);
  delay(3000);
#ifdef HUMIDITY

  float h = dht.readHumidity();

  float t = dht.readTemperature();

  if (isnan(h) || isnan(t))
  {
    digitalWrite(LED_AWAIT, LOW);
    return;
  }
#endif

  *hum = h;
  *temp = t;

  digitalWrite(LED_AWAIT, LOW);
}

void powerSleep()
{
  Serial.println("sleep-power1");

  radio.powerDown();
  delay(50);
  delay(50);
  digitalWrite(RF_POWER, LOW); // disable 5v -> 3.3v
  if (usePowerManagement)
    power.sleepDelay(20 * 1000);
  else
    delay(2 * 1000);
  digitalWrite(RF_POWER, HIGH); // enable 5v -> 3.3v
  delay(50);
  setupRf();
  radio.powerUp();
}

void setup()
{
  Serial.begin(9600);
  Serial.println("setup");
  pinMode(LED_AWAIT, OUTPUT);
  pinMode(LED_RELAY, OUTPUT);
  pinMode(LED_STATUS, OUTPUT);
  pinMode(RF_POWER, OUTPUT);

  digitalWrite(LED_AWAIT, HIGH);
  delay(50);
  digitalWrite(RF_POWER, HIGH);

  delay(2000);
  printf_begin();
  setup_power();
  setupRf();
  radio.printDetails();

#if HUMIDITY
  dht.begin();
#endif
  radio.startListening();
  radio.stopListening();

  digitalWrite(LED_AWAIT, LOW);
  delay(1000);
}
void loop(void)
{
  float temp = -1;
  float hum = -1;
  record_metrics(&hum, &temp);
  Payload a;
  a.pkgID = PACKAGE_TEMPERATURE;
  a.data = temp;

  bool result = radio.write(&a, sizeof(Payload));

  a.pkgID = PACKAGE_HUMIDITY;
  a.data = hum;

  delay(500);

  result &= radio.write(&a, sizeof(Payload));

  if (result)
  {
    digitalWrite(LED_RELAY, HIGH);
    Serial.println("Success transmitting payload.");
    delay(1500);
  }
  else
  {
    Serial.println("Failed transmitting payload.");
    signal_error(LED_STATUS, 10);
    delay(1000);
  }
  delay(1500);
  digitalWrite(LED_RELAY, 0);
  digitalWrite(LED_STATUS, 0);
  delay(1500);

  powerSleep();
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
#endif