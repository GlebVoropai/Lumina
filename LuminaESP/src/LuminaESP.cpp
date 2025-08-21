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

enum State { WAIT_WIFI, GREEN_FADE, NORMAL_OPERATION };
State currentState = WAIT_WIFI;

// --- Fade variables ---
uint8_t brightness = 0;
bool increasing = true;
unsigned long prevTime = 0;
const unsigned long fadeInterval = 30; // мс

// --- UDP timeout ---
unsigned long lastPacketTime = 0;
const unsigned long packetTimeout = 2000; // 2 сек

// --- HSL to RGB function ---
CRGB HSLtoRGB(float h, float s, float l) {
  float c = (1.0f - fabs(2.0f * l - 1.0f)) * s;
  float x = c * (1.0f - fabs(fmod(h / 60.0f, 2) - 1.0f));
  float m = l - c / 2.0f;
  float r1, g1, b1;

  if (h < 60)       { r1 = c; g1 = x; b1 = 0; }
  else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
  else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
  else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
  else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
  else              { r1 = c; g1 = 0; b1 = x; }

  uint8_t r = (uint8_t)((r1 + m) * 255);
  uint8_t g = (uint8_t)((g1 + m) * 255);
  uint8_t b = (uint8_t)((b1 + m) * 255);

  return CRGB(r, g, b);
}

void setup() {
  Serial.begin(115200);
  FastLED.addLeds<WS2812, LED_PIN, GRB>(leds, NUM_LEDS);
  FastLED.clear();
  FastLED.show();

  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  Serial.print("Connecting to WiFi");
}

void loop() {
  // --- WiFi check ---
  if (currentState == WAIT_WIFI && WiFi.status() == WL_CONNECTED) {
    Serial.println("\nWiFi connected: " + WiFi.localIP().toString());
    udp.begin(UDP_PORT);
    Serial.printf("UDP listening on port %d\n", UDP_PORT);
    currentState = GREEN_FADE;
    brightness = 0;
    increasing = true;
    prevTime = millis();
  }

  // --- Smooth green fade ---
  if (currentState == GREEN_FADE) {
    unsigned long now = millis();
    if (now - prevTime >= fadeInterval) {
      prevTime = now;

      fill_solid(leds, NUM_LEDS, CRGB(0, brightness, 0));
      FastLED.show();

      if (increasing) {
        brightness += 5;
        if (brightness >= 255) increasing = false;
      } else {
        brightness -= 5;
        if (brightness == 0) {
          currentState = NORMAL_OPERATION;
        }
      }
    }
  }

  // --- UDP Handling ---
  if (currentState == NORMAL_OPERATION) {
    int packetSize = udp.parsePacket();
    if (packetSize == NUM_LEDS * 3) {
      byte data[NUM_LEDS * 3];
      udp.read(data, NUM_LEDS * 3);

      for (int i = 0; i < NUM_LEDS; i++) {
        // Предполагаем, что приходят H, S, L в диапазоне 0-255
        float h = (float)data[i * 3] / 255.0f * 360.0f;
        float s = (float)data[i * 3 + 1] / 255.0f;
        float l = (float)data[i * 3 + 2] / 255.0f;

        leds[i] = HSLtoRGB(h, s, l);
      }

      FastLED.show();
      lastPacketTime = millis();
    }

    // --- Timeout check ---
    if (millis() - lastPacketTime > packetTimeout) {
      FastLED.clear();
      FastLED.show();
    }
  }
}
