
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