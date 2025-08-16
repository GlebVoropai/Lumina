#include <Arduino.h>
#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <FastLED.h>
#include "keys.h"

#define LED_PIN D1
#define NUM_LEDS 60
#define UDP_PORT 4210

CRGB leds[NUM_LEDS];
WiFiUDP udp;

bool wifiReady = false;

// --- Fade ---
uint8_t brightness = 0;
bool increasing = true;
unsigned long prevTime = 0;
bool fadeDone = false;

void setup() {
  Serial.begin(115200);
  FastLED.addLeds<WS2812, LED_PIN, GRB>(leds, NUM_LEDS);
  FastLED.clear(true);

  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  Serial.print("Connecting to WiFi");
}

void loop() {
  // --- WiFi connect ---
  if (!wifiReady && WiFi.status() == WL_CONNECTED) {
    Serial.println("\nWiFi connected: " + WiFi.localIP().toString());
    udp.begin(UDP_PORT);
    wifiReady = true;
    prevTime = millis();
    brightness = 0;
    increasing = true;
    fadeDone = false;
  }

  // --- One green fade after WiFi connect ---
  if (wifiReady && !fadeDone) {
    unsigned long now = millis();
    if (now - prevTime >= 30) {
      prevTime = now;
      fill_solid(leds, NUM_LEDS, CRGB(0, brightness, 0));
      FastLED.show();

      if (increasing) {
        brightness += 5;
        if (brightness >= 255) increasing = false;
      } else {
        if (brightness <= 5) { // дошли до нуля
          brightness = 0;
          fadeDone = true;
          FastLED.clear(true);
        } else {
          brightness -= 5;
        }
      }
    }
  }

  // --- UDP Handling ---
  if (wifiReady && fadeDone) {
    int packetSize = udp.parsePacket();
    if (packetSize == 3) {
      byte rgb[3];
      udp.read(rgb, 3);
      fill_solid(leds, NUM_LEDS, CRGB(rgb[0], rgb[1], rgb[2]));
      FastLED.show();
    }
  }
}
