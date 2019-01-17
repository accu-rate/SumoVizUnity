﻿using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class PedestrianMover : MonoBehaviour {

    private bool initialized = false;
    private GameObject peds;
    private bool playing = false;
    private int roundCounter = 0;
    private PointerEventData eventData;
    private float speedFactor = 1;

    private float currentTime;
    private float currentRealTime;
    private float maxTime = 0;
    // for recording purpose: only first round is recorded
    private bool firstRound;


    // set in inspector
    [SerializeField] Sprite PauseSprite;
    [SerializeField] Sprite PlaySprite;
    [SerializeField] Button playButton;
    [SerializeField] Slider slider;
    [SerializeField] Text startTime;
    [SerializeField] Text endTime;
    [SerializeField] float renderStep = 1;
    [SerializeField] Slider playbackSpeed;
    [SerializeField] InputField renderSpeedField;

    public PedestrianMover() { }

    private void Start() {
        currentTime = 0.1f;
        playButton.onClick.AddListener(delegate () { this.changePlaying(); });
    }

    internal bool isFirstRound() {
        return firstRound;
    }

    internal void init(SimData simData) {
        peds = simData.getPedestrianGameObject();
        foreach (Transform ped in peds.transform) {
            ped.GetComponent<Pedestrian>().init();
        }

        initialized = true;
        roundCounter = 0;
        currentTime = 0.1f;
        currentRealTime = 0;
        maxTime = simData.getMaxTime();
        firstRound = true;

        endTime.text = maxTime.ToString();
        startTime.text = currentTime.ToString();
        slider.value = 0;
    }

    public void changePlaying() {
        if (playing) {
            playing = false;
            playButton.image.sprite = PlaySprite;
        } else {
            playing = true;
            playButton.image.sprite = PauseSprite;
        }
    }

    public void Reset() {
        playing = false;
        initialized = false;
        playButton.image.sprite = PlaySprite;
    }

    public void Update() {

        if (playing && initialized) {
            // old approach
            currentRealTime = currentRealTime + Time.deltaTime;
 
            if (currentTime >= maxTime) { // new round
                currentTime = 0.1f;
                currentRealTime = 0;
                firstRound = false;

                foreach (Transform ped in peds.transform) {
                    ped.GetComponent<Pedestrian>().reset();
                }
                roundCounter++;
            }


            if (initialized && currentRealTime * speedFactor >= currentTime ) {
                // render each second
                currentTime = currentTime + renderStep;
                foreach (Transform ped in peds.transform) {
                    ped.GetComponent<Pedestrian>().move(currentTime);
                }
                updateSlider();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            playing = !playing;
        }

    }

    internal float getCurrentTime() {
        return currentTime;
    }

    private void updateSlider() {
        float lerpValue = currentTime / maxTime;
        slider.value = Mathf.Lerp(0f, 1f, (float)lerpValue);
        startTime.text = currentTime.ToString("0.##");
    }

    public void dragSlider(BaseEventData ev) {
        currentTime = slider.value * maxTime;
        currentRealTime = slider.value * maxTime;
        startTime.text = currentTime.ToString("0.##");
        if (initialized) {
            foreach (Transform ped in peds.transform) {
                ped.GetComponent<Pedestrian>().move(currentTime);
            }
        }
    }

    public void changeSpeed(BaseEventData ev) {
        speedFactor = playbackSpeed.value;
        currentRealTime = currentTime;
    }

    public void changeRenderSpeed(String newValue) {
        if (!float.TryParse(newValue, out renderStep)) {
            renderStep = 1;
        }
        if (renderStep < 0) {
            renderStep = Math.Abs(renderStep);
        }
    }
}