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
          currentState = NORMAL_OPERATION; // закончили один плавный миг
        }
      }
    }
  }

  // --- UDP Handling ---
  if (currentState == NORMAL_OPERATION) {
    int packetSize = udp.parsePacket();
    if (packetSize == NUM_LEDS * 3) {
      byte rgb[NUM_LEDS * 3];
      udp.read(rgb, NUM_LEDS * 3);

      for (int i = 0; i < NUM_LEDS; i++) {
        leds[i] = CRGB(rgb[i * 3], rgb[i * 3 + 1], rgb[i * 3 + 2]);
      }

      FastLED.show();
      lastPacketTime = millis(); // обновляем время приёмa пакета
    }

    // --- Timeout check ---
    if (millis() - lastPacketTime > packetTimeout) {
      FastLED.clear();
      FastLED.show();
    }
  }
}
