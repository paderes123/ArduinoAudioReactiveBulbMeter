const int lightPins[] = {2, 3, 4, 5, 6, 7, 8, 9}; // 8 lights
const int numOfLights = 8;

unsigned long lastDataTime = 0;
const unsigned long dataTimeout = 100; // ms before clearing

bool lightState[numOfLights] = {false}; // false = OFF, true = ON

void setup() {
  Serial.begin(9600);

  for (int i = 0; i < numOfLights; i++) {
    pinMode(lightPins[i], OUTPUT);
    digitalWrite(lightPins[i], HIGH); // OFF by default (active LOW)
  }
}

void audioVisualizerLight(int level) {
  for (int i = 0; i < numOfLights; i++) {
    lightState[i] = (i < level);
    digitalWrite(lightPins[i], lightState[i] ? LOW : HIGH); // active LOW
  }
}

void loop() {
  if (Serial.available() > 0) {

    String data = Serial.readStringUntil('\n');
    data.trim();
    lastDataTime = millis();

    int numericLevel = data.toInt();

    if (numericLevel > 0) {
      audioVisualizerLight(numericLevel);
    } else {
      audioVisualizerLight(0);
    }
  }

  // Auto-clear when no new data
  if (millis() - lastDataTime > dataTimeout) {
    audioVisualizerLight(0);
  }
}
