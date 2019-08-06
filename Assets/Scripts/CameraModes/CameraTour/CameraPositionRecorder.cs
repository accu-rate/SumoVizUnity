using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using System;
using SFB;
using System.Globalization;


// inspired from: https://answers.unity.com/questions/585314/record-camera-and-play-again.html

public class CameraPositionRecorder : MonoBehaviour {

    [System.Serializable]
    //simple class for values we're going to record
    public class GoVals {
        public Vector3 position;
        public Quaternion rotation;
        public float frame;

        //constructor
        public GoVals(Vector3 position, Quaternion rotation, float frame) {
            this.position = position;
            this.rotation = rotation;
            this.frame = frame;
        }

        //constructor
        public GoVals(Vector3 position, Quaternion rotation) {
            this.position = position;
            this.rotation = rotation;
            this.frame = -1;
        }

    }

    //a list of recorded values
    SortedList<float, GoVals> vals = new SortedList<float, GoVals>();


    [SerializeField] Button addCameraPosition;
    [SerializeField] Button removeCameraPosition;
    [SerializeField] Button loadCameraPosition;
    [SerializeField] Button saveCameraPosition;
    [SerializeField] Button resetPositions;

    [SerializeField] GameObject cameraPositionTable;
    [SerializeField] GameObject columnsPrefab;
 
    private PedestrianMover pm;
    private GoVals currentPoint;
    private float currentFrame;

    private int currentIndex;
    private StreamWriter writer;

    private int noOfCameraPositions = 1;
    private int yOffset = 15;

    //cache of our transform
    Transform tf;

    void Start() {
        //cache it...
        tf = this.transform;
        addCameraPosition.onClick.AddListener(delegate () {
            this.addPosition();
        });

        removeCameraPosition.onClick.AddListener(delegate () {
            this.removePosition();
        });


        loadCameraPosition.onClick.AddListener(delegate () {
            this.loadCameraPositions();
        });

        saveCameraPosition.onClick.AddListener(delegate () {
            this.saveCameraPositions();
        });

        resetPositions.onClick.AddListener(delegate () {
            this.Reset();
        });
        pm = FindObjectOfType<PedestrianMover>();
    }

    private void saveCameraPositions() {
        String savedPositions = StandaloneFileBrowser.SaveFilePanel("Save File", "", "", "txt"); //Path.GetFileName(path))
        if (savedPositions == "") // = cancel was clicked in open file dialog
            return;
        writer = new StreamWriter(savedPositions, false);
        writer.AutoFlush = true;
        foreach  (GoVals val in  vals.Values) {
            writer.WriteLine(val.position.x + ";" + val.position.y + ";" + val.position.z + ";" + val.rotation.x + ";" + val.rotation.y + ";" + val.rotation.z + ";" + val.rotation.w + ";" + val.frame.ToString());

        }
        writer.Close();
    }

    private void loadCameraPositions() {
        vals = new SortedList<float, GoVals>();
        String[] savedPositions = StandaloneFileBrowser.OpenFilePanel("", "", "txt;*.txt", false); 
        if (savedPositions == null) // = cancel was clicked in open file dialog
            return;
        String savedPositionFile = savedPositions[0];
        StreamReader file = new StreamReader(savedPositionFile);
        string line;
        while ((line = file.ReadLine()) != null) {
            string[] values = line.Split(';');
            if (values.Length >= 4) {
                Vector3 position;
                Quaternion rotation;
                float currentTime;
                int id;
                float x, y, z, r;
                float.TryParse(values[0], out x);
                float.TryParse(values[1],  out y);
                float.TryParse(values[2],  out z);
                position = new Vector3(x, y, z);

                float.TryParse(values[3], out x);
                float.TryParse(values[4],  out y);
                float.TryParse(values[5],  out z);
                float.TryParse(values[6],  out r);
                rotation = new Quaternion(x, y, z, r);

                float.TryParse(values[7], out currentTime);
                createPosition(position, rotation, currentTime);
            }
        }
        createTableEntries();
    }


    private void createPosition(Vector3 position, Quaternion rotation, float currentTime) {
        GoVals value = new GoVals(position, rotation, currentTime);
        if (vals.ContainsKey(currentTime))
            return;

        vals.Add(currentTime, value);
        if (currentPoint == null) {
            currentPoint = value;
            currentIndex = 0;
        }
   }

    private void addPosition() {
        createPosition(tf.position, tf.rotation, pm.getCurrentTime());
        createTableEntries();
    }


    private void removePosition() {

        if (vals.Count > 0) { 
          GoVals lastPosition = vals.Values[vals.Count - 1];
          vals.Remove(lastPosition.frame);
        }

        createTableEntries();
    }


    private void createTableEntries() {
        // from: https://forum.unity.com/threads/deleting-all-chidlren-of-an-object.92827/ otherwise only every other object will be deleted
        var children = new List<GameObject>();
        foreach (Transform child in cameraPositionTable.transform) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        noOfCameraPositions = 1;

        foreach (GoVals camerapos in vals.Values) {
            createTableEntry(camerapos.frame);

        }
     }


    private void createTableEntry(float frame) {

        GameObject newcolumn = Instantiate(columnsPrefab,
            new Vector3(columnsPrefab.transform.position.x, columnsPrefab.transform.position.y + yOffset * noOfCameraPositions, columnsPrefab.transform.position.z),
            Quaternion.identity);
        newcolumn.transform.SetParent(cameraPositionTable.transform);

        // add table entry
        newcolumn.transform.Find("Label").gameObject.GetComponent<Text>().text = "Position " + noOfCameraPositions;

        TimeSpan currentTime = TimeSpan.FromSeconds(frame);
        newcolumn.transform.Find("Time").gameObject.GetComponent<Text>().text = currentTime.ToString(@"hh\:mm\:ss");

        noOfCameraPositions++;
    }


    void Update() {
        ReplayPoints();
    }


    void ReplayPoints() {
        if (!pm.isInReplayMode())
            return;

        if (!pm.isPlaying()) {
            pm.resetSlider();
            pm.changePlaying();
        }

        if (vals.Count == 0)
            return;

        // reset for first point, if slider was dragged backwards
        if (currentFrame > pm.getCurrentTime()) {
            currentPoint = vals.Values[0];
            currentIndex = 0;
        }
        currentFrame = pm.getCurrentTime();

        //if no further camera points are stored, stay at this position and stop replaying
        if (pm.getCurrentTime() >= vals.Values[vals.Count - 1].frame) {
            currentIndex = 0;
            currentPoint = vals.Values[currentIndex];

            // set to last camera position
            tf.position = vals.Values[vals.Count - 1].position;
            tf.rotation = vals.Values[vals.Count - 1].rotation;
            return;
        } else if (pm.getCurrentTime() >= currentPoint.frame) {
            //set our transform values
            tf.position = currentPoint.position;
            tf.rotation = currentPoint.rotation;
            currentIndex = currentIndex + 1;
            currentPoint = vals.Values[currentIndex];
        } else if (currentIndex > 0) {
            float timeBetweenPts = vals.Values[currentIndex].frame - vals.Values[currentIndex - 1].frame;
            float ratio = (pm.getCurrentTime() - vals.Values[currentIndex - 1].frame) / timeBetweenPts;
            tf.position = Vector3.Lerp(vals.Values[currentIndex - 1].position, vals.Values[currentIndex].position, ratio);
            tf.rotation = Quaternion.Lerp(vals.Values[currentIndex - 1].rotation, vals.Values[currentIndex].rotation, ratio);
        }
    }

    public void prepareForReplaying() {
        addCameraPosition.enabled = false;
        saveCameraPosition.enabled = false;
        loadCameraPosition.enabled = false;
        resetPositions.enabled = false;
        removeCameraPosition.enabled = false;
    }

    public void stopForReplaying() {
        addCameraPosition.enabled = true;
        saveCameraPosition.enabled = true;
        loadCameraPosition.enabled = true;
        resetPositions.enabled = true;
        removeCameraPosition.enabled = true;

    }


    public void Reset() {
        vals = new SortedList<float, GoVals>();
        currentPoint = null;
    }
}

