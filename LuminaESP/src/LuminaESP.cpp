#include <Arduino.h>
#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <FastLED.h>
#include "keys.h"
// Settings
#define LED_PIN D1
#define NUM_LEDS 60
#define UDP_PORT 4210

CRGB leds[NUM_LEDS];
WiFiUDP udp;

void setup() {
  Serial.begin(115200);
  FastLED.addLeds<WS2812, LED_PIN, GRB>(leds, NUM_LEDS);

  // Connecting to WiFi
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  Serial.print("Connecting to WiFi");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nWiFi connected: " + WiFi.localIP().toString());

  // Start listening UDP
  udp.begin(UDP_PORT);
  Serial.printf("UDP listening on port %d\n", UDP_PORT);
}

void loop() {
  int packetSize = udp.parsePacket();
  if (packetSize) {
    char incomingPacket[3];
    int len = udp.read(incomingPacket, 3);
    if (len == 3) {
      byte r = incomingPacket[0];
      byte g = incomingPacket[1];
      byte b = incomingPacket[2];
      for (int i = 0; i < NUM_LEDS; i++) {
        leds[i] = CRGB(r, g, b);
      }
      FastLED.show();
    }
  }
}
