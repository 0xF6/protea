#define CLIENT
#include <Arduino.h>
#include "RF24.h"
#include "printf.h"

#ifdef CLIENT
#define LED_STATUS A1
#else
#define LED_STATUS LED_BUILTIN
#endif

#include "vcc.h"
vcc voltage;

RF24 radio(9, 10, 10000000U);

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
  byte type;
  float temp;
  float hum;
  float co2;
  float volt;
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
  printf_begin();
  radio.setCRCLength(RF24_CRC_8);
  //radio.disableCRC();
  radio.disableAckPayload();
  radio.setAutoAck(false);
  radio.setDataRate(RF24_250KBPS);
  radio.setPALevel(RF24_PA_HIGH);
  radio.setPayloadSize(sizeof(byte));

  radio.setChannel(0);
  radio.openReadingPipe(0, 0xAAAAAAAAAALL);
  radio.openReadingPipe(1, 0xBFB4B5B601LL);
  radio.openReadingPipe(2, 0xBFB4B5B602LL);
  radio.openReadingPipe(3, 0xBFB4B5B603LL);
  radio.openReadingPipe(4, 0xBFB4B5B604LL);
  radio.openReadingPipe(5, 0xBFB4B5B605LL);

  radio.startListening();
  radio.printPrettyDetails();
  radio.printDetails();
  Serial.println(F("TEMPERATURE, HUMIDITY, VOLTAGE, CO2"));
}
void setup()
{
  Serial.begin(9600);
  printf_begin();
  pinMode(A2, INPUT);
  pinMode(A3, INPUT);
  pinMode(A4, INPUT);
  // pinMode(LED_AWAIT, OUTPUT);
  // pinMode(LED_RELAY, OUTPUT);
  pinMode(LED_STATUS, OUTPUT);

  setupRf();
}
void error()
{
  // Serial.println("No data");
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
    /*
    Payload a;
    radio.read(&a, sizeof(Payload));

    if (a.hash_0 != PAYLOAD_HASH_0 || a.hash_1 != PAYLOAD_HASH_1)
      return;

    Serial.print(a.temp);
    Serial.print(F(", "));
    Serial.print(a.hum);
    Serial.print(F(", "));
    Serial.print(a.volt);
    Serial.print(F(", "));
    Serial.println(a.co2);*/
    uint8_t pipe;
    if(radio.available(&pipe)) {
      byte a;
      radio.read(&a, sizeof(byte));
      Serial.print("PIPE: ");
      Serial.print(pipe);
      Serial.print(" DATA: ");
      Serial.println(a);
      Serial.print("Payload: ");
      Serial.println((byte)sizeof(Payload));
      Serial.print("Float: ");
      Serial.println((byte)sizeof(float));
      Serial.print("Byte: ");
      Serial.println((byte)sizeof(byte));
    }
    delay(700);
  }
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


#include <OneWire.h>
#include <Wire.h>


#include <Adafruit_Sensor.h>
#include <DHT.h>
#include <DHT_U.h>

#define DHTTYPE DHT21
#define DHTPIN 7

DHT dht(DHTPIN, DHTTYPE);

#endif

#define PIN_CO2 5
#define PIN_NH3 6
#define PIN_NO3 7


#define PWR_ALL 0xFFFF
#define PWR_USB _BV(15)
#define PWR_TIMER5 _BV(13)
#define PWR_TIMER4 _BV(12)
#define PWR_TIMER3 _BV(11)
#define PWR_UART3 _BV(10)
#define PWR_UART2 _BV(9)
#define PWR_UART1 _BV(8)
#define PWR_I2C _BV(7)
#define PWR_TIMER2 _BV(6)
#define PWR_TIMER0 _BV(5)
#define PWR_TIMER1 _BV(3)
#define PWR_SPI _BV(2)
#define PWR_UART0 _BV(1)
#define PWR_ADC _BV(0)

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
  power.autoCalibrate();
power.hardwareDisable(
    PWR_ADC |
    PWR_UART0	 |
    PWR_I2C	  |
    PWR_USB); // disable all hardware
  power.setSleepMode(POWERDOWN_SLEEP);
  power.bodInSleep(false);
  signal_await();
}

void setupRf()
{
  delay(1500);
  if (!radio.begin())
  {
    while (1)
    {
      signal_error(LED_STATUS, 3);
      Serial.println(F("radio hardware is not responding!!"));
      if (radio.begin())
        break;
    }
  }
  radio.setCRCLength(RF24_CRC_8);
  radio.setDataRate(RF24_250KBPS);
  radio.setPALevel(RF24_PA_HIGH);
  radio.setPayloadSize(sizeof(Payload));

  radio.setChannel(0);
  //radio.openWritingPipe(0xBFB4B5B600LL);
  radio.openWritingPipe(0xAAAAAAAAAALL);
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
  radio.powerDown();
  delay(50);
  digitalWrite(RF_POWER, LOW); // disable 5v -> 3.3v
  delay(50);
  if (usePowerManagement)
  {
    power.sleepDelay(30 * 1000);
    power.sleepDelay(30 * 1000);
  }
  else
    delay(2 * 1000);
  digitalWrite(RF_POWER, HIGH); // enable 5v -> 3.3v
  delay(50);
  setupRf();
  radio.powerUp();
}

Payload a;

void setup()
{
  Serial.begin(9600);
  pinMode(LED_AWAIT, OUTPUT);
  pinMode(LED_RELAY, OUTPUT);
  pinMode(LED_STATUS, OUTPUT);
  pinMode(RF_POWER, OUTPUT);

  digitalWrite(LED_AWAIT, HIGH);
  delay(50);
  digitalWrite(RF_POWER, HIGH);

  delay(2000);
  printf_begin();
  //setup_power();
  setupRf();
  radio.printDetails();

#if HUMIDITY
  dht.begin();
#endif
  radio.startListening();
  radio.stopListening();

  digitalWrite(LED_AWAIT, LOW);
  delay(1000);
  a.hash_0 = PAYLOAD_HASH_0;
  a.hash_1 = PAYLOAD_HASH_1;
}



void loop(void)
{
  float temp = -1;
  float hum = -1;
  record_metrics(&hum, &temp);

  //power.hardwareEnable(PWR_ADC);
  delayMicroseconds(250);
  a.volt = voltage.read_volts();


  a.co2 = -1;
  a.hum = hum;
  a.temp = temp;

  //power.hardwareDisable(PWR_ADC);


  bool result = radio.write(&a, sizeof(Payload));

  if (result)
  {
    digitalWrite(LED_RELAY, HIGH);
    Serial.println("Success transmitting payload.");
    delay(500);
  }
  else
  {
    Serial.println("Failed transmitting payload.");
    signal_error(LED_STATUS, 2);
    delay(1000);
  }
  //delay(1500);
  digitalWrite(LED_RELAY, 0);
  digitalWrite(LED_STATUS, 0);
  delay(1500);

  //powerSleep();
}
#endif