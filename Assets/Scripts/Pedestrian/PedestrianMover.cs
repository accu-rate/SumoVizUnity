using System;
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
//    private float currentRealTime;
    private float maxTime = 0;
    // for recording purpose: only first round is recorded
    private bool firstRound;


    // set in inspector
    [SerializeField] Sprite PauseSprite;
    [SerializeField] Sprite PlaySprite;
    [SerializeField] Button playButton;
    [SerializeField] Slider slider;
    [SerializeField] Text playbackSpeedText;
    [SerializeField] Text startTime;
    [SerializeField] Text endTime;
    [SerializeField] float renderStep;
    [SerializeField] Slider playbackSpeed;
    [SerializeField] InputField renderSpeedField;

    public PedestrianMover() { }

    private void Start() {
        currentTime = 0.1f;
        playButton.onClick.AddListener(delegate () { this.changePlaying(); });

        // TODO: We do not want to get a differnt value for render speed, let unity handle this.
        renderSpeedField.gameObject.SetActive(false);
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
        maxTime = simData.getMaxTime();
        firstRound = true;

        playbackSpeed.maxValue = 100;
        playbackSpeedText.text = playbackSpeed.value.ToString("#.#") + "x";

        //       endTime.text = maxTime.ToString();
        //        startTime.text = currentTime.ToString();
        TimeSpan time = TimeSpan.FromSeconds(maxTime);
        TimeSpan starttime = TimeSpan.FromSeconds(currentTime);

        //here backslash is must to tell that colon is
        //not the part of format, it just a character that we want in output
        endTime.text = time.ToString(@"hh\:mm\:ss");
        startTime.text = starttime.ToString(@"hh\:mm\:ss");

        slider.value = 0;
        renderStep = 0.1f;
    }

    public void changePlaying() {
        if (playing) {
            playButton.image.sprite = PlaySprite;
            playButton.transform.Find("PlayText").gameObject.GetComponent<Text>().text = "Play";
            playing = false;
        } else {
            playing = true;
            playButton.image.sprite = PauseSprite;
            playButton.transform.Find("PlayText").gameObject.GetComponent<Text>().text = "Pause";
        }
        foreach (Transform ped in peds.transform) {
            ped.GetComponent<Pedestrian>().pause(playing);
        }

    }

    public bool isPlaying() {
        return playing;
    }

    public void Reset() {
        playing = false;
        initialized = false;
        playButton.image.sprite = PlaySprite;
    }

    public void Update() {

        if (playing && initialized) {
            // old approach
            currentTime = (currentTime + Time.deltaTime * speedFactor) ;

            if (currentTime >= maxTime) { // new round
                currentTime = 0;
                firstRound = false;

                foreach (Transform ped in peds.transform) {
                    ped.GetComponent<Pedestrian>().reset();
                }
                roundCounter++;
            }

             foreach (Transform ped in peds.transform) {
                ped.GetComponent<Pedestrian>().move(currentTime);
            }
            updateSlider();
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
        TimeSpan starttime = TimeSpan.FromSeconds(currentTime);
        startTime.text = starttime.ToString(@"hh\:mm\:ss");
    }

    public void dragSlider(BaseEventData ev) {
        dragSlider();
    }

    private void dragSlider() {
        currentTime = slider.value * maxTime;
        TimeSpan starttime = TimeSpan.FromSeconds(currentTime);
        startTime.text = starttime.ToString(@"hh\:mm\:ss");
        if (initialized) {
            foreach (Transform ped in peds.transform) {
                ped.GetComponent<Pedestrian>().move(currentTime);
                ped.GetComponent<Pedestrian>().pause(playing);
            }
        }
    }

    public void resetSlider() {
        slider.value = slider.minValue;
        dragSlider();
    }

    public void changeSpeed(BaseEventData ev) {
        speedFactor = playbackSpeed.value;
        playbackSpeedText.text = speedFactor.ToString("#.#") + "x";
    }

    private void changeSpeed() {
        speedFactor = playbackSpeed.value;
        playbackSpeedText.text = speedFactor.ToString("#.#") + "x";
    }

    public void resetSpeed() {
        playbackSpeed.value = playbackSpeed.minValue;
        changeSpeed();
    }

    public void changeRenderSpeed(String newValue) {
        renderSpeedField.text = newValue;

        if (!float.TryParse(newValue, out renderStep)) {
            renderStep = 0.1f;
            renderSpeedField.text = "0.1";
        }
        if (renderStep < 0) {
            renderStep = Math.Abs(renderStep);
            renderSpeedField.text = renderStep.ToString();
        }
    }
}
