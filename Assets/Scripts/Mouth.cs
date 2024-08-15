using Mediapipe.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This process data from MediaPipe running in the scene, to provide access to scores representing how likely the shape of lips is the same as provided shapes <br></br>
/// Depends on MediaPipe Unity Plugin (version: 0.14.4) <br></br>
/// </summary>
public class Mouth : MonoBehaviour
{
    /// <summary>
    /// instance Mouth object created for access to data on shapes computed
    /// </summary>
    public static Mouth Instance;

    [SerializeField, Tooltip("reference to Mediapipe Facemesh controller")] private MultiFaceLandmarkListAnnotationController s_landmarkController;
    //[SerializeField] private TextMeshProUGUI infoText;

    [Space(10f)] [Header("lines")]
    [SerializeField, Tooltip("positions of landmarks of outer line of top lip, represented by list of vector3 in world coordinates")] private List<Vector3> upOutline = new();
    [SerializeField, Tooltip("positions of landmarks of inner line of top lip, represented by list of vector3 in world coordinates")] private List<Vector3> upInline = new();
    [SerializeField, Tooltip("positions of landmarks of inner line of bottom lip, represented by list of vector3 in world coordinates")] private List<Vector3> bottomInline = new();
    [SerializeField, Tooltip("positions of landmarks of outer line of bottom lip, represented by list of vector3 in world coordinates")] private List<Vector3> bottomOutline = new();

    [Space(10f)] [Header("transforms")]
    [SerializeField, Tooltip("list of transforms reference of landmarks of outer line of top lip")] private List<Transform> upOutTransform = new();
    [SerializeField, Tooltip("list of transforms reference of landmarks of inner line of top lip")] private List<Transform> upInTransform = new();
    [SerializeField, Tooltip("list of transforms reference of landmarks of inner line of bottom lip")] private List<Transform> bottomInTransform = new();
    [SerializeField, Tooltip("list of transforms reference of landmarks of outer line of bottom lip")] private List<Transform> bottomOutTransform = new();

    [Space(10f)] [Header("info")]
    [SerializeField, Tooltip("current value of angle from center to right-most point of outer line of top lip in degree")] private float c_rightUpDeg;
    [SerializeField, Tooltip("current value of angle from center to left-most point of outer line of top lip in degree")] private float c_leftUpDeg;
    [SerializeField, Tooltip("current value of angle from center to right-most point of outer line of bottom lip in degree")] private float c_rightBottomDeg;
    [SerializeField, Tooltip("current value of angle from center to left-most point of outer line of bottom lip in degree")] private float c_leftBottomDeg;
    [SerializeField, Tooltip("current value of height of gap between centers of inner lines of top lip & bottom lip")] private float c_height;
    [SerializeField, Tooltip("current value of width of gap between left-most and right-most points of lips")] private float c_width;
    [SerializeField, Tooltip("current value of ratio of height to width")] private float c_heightWidthRatio;
    [SerializeField, Tooltip("list of avg value related info")] private List<VarInfo> varInfos;
    [SerializeField, Tooltip("list of avg value related info only for recording base value of shape")] private List<VarInfo> baseVarInfos;

    [Space(10f)] [Header("shape")]
    [SerializeField, Tooltip("array of base values related info of shapes")] private ShapeScore[] shapeScores;

    [Space(10f)] [Header("sampling")]
    [SerializeField, Tooltip("frequency of recording value for computing avg values")] private float samplingFrequency = 30f;
    [SerializeField, Tooltip("number of records considered for computing avg values")] private int samplingAmount = 30;
    [SerializeField, Tooltip("number of records considered for computing new base value")] private int baseValueSamplingAmount = 100;
    [SerializeField, Tooltip("time in seconds for user to prepare before starting to record for new base value")] private float baseValueReadyDuration = 3f;

    [Space(10f)] [Header("display")]
    [SerializeField, Tooltip("transform of the gameObject holding display items")] private Transform t_displayItemList;
    [SerializeField, Tooltip("threashold to highlight item when corresponding shape type has high enough score")] private float threashold = 0.8f;
    [SerializeField, Tooltip("max difference between one shape type from the shape type with highest score to be highlighted")] private float maxRange = 0.2f;

    [Space(10f)] [Header("test")]
    [SerializeField, Tooltip("shape type to be used as parameter")] private ShapeType testShapeType;
    [SerializeField, Tooltip("invoke function to set new base value of testShapeType with current values")] private bool callSetBaseInfo;
    [SerializeField, Tooltip("invoke function to set new base value of testShapeType with recorded avg values")] private bool callSetBaseAvgInfo;
    [SerializeField, Tooltip("invoke functions to set new base value of all shape types not disabled with recorded avg values")] private bool callSetAllBaseAvgInfo;
    
    // this represents the points in lines that are considered
    // check the index of point annotation representing the point needed inside [Main Canvas -> Container Panel -> Body -> Annotatable Screen -> Annotation Layer -> MultiFaceLandmarks Annotation -> FaceLandmarkListWithIris Annotation(Clone) -> FaceLandmarkList Annotation -> Point List Annotation]
    // or go to Mediapipe.Unity.FaceLandmarkListAnnotation.cs for '_connections' representing connections of points representing parts of face
    private List<List<int>> linesIndex = new List<List<int>>
    {
        //right to left
        new List<int>{61, 185, 40, 39, 37, 0, 267, 269, 270, 409, 291, },//up out
        new List<int>{78, 191, 80, 81, 82, 13, 312, 311, 310, 415, 308, },//up in
        new List<int>{78, 95, 88, 178, 87, 14, 317, 402, 318, 324, 308, },//bottom in
        new List<int>{61, 146, 91, 181, 84, 17, 314, 405, 321, 375, 291, },//bottom out
    };

    void Awake()
    {
        // set reference for others to use
        if (Mouth.Instance == null) Mouth.Instance = this;

        if (s_landmarkController == null) 
        { 
            s_landmarkController = FindObjectOfType<MultiFaceLandmarkListAnnotationController>();
            if (s_landmarkController == null)
            {
                Debug.Log("no mediapipe face landmark controller found");
            }
        }

        // reset record list for avg value
        varInfos.Clear();
        Array.ForEach((ScoreVariableType[])Enum.GetValues(typeof(ScoreVariableType)), x => varInfos.Add(new VarInfo(x)));
        // reset record list for base avg value
        baseVarInfos.Clear();
        Array.ForEach((ScoreVariableType[])Enum.GetValues(typeof(ScoreVariableType)), x => baseVarInfos.Add(new VarInfo(x)));
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            //call setting base value of selected shape on current frame
            if (callSetBaseInfo) { callSetBaseInfo = false; SetBaseValue(testShapeType); }
            //call setting base value of selected shape on past frames
            if (callSetBaseAvgInfo) { callSetBaseAvgInfo = false; StartCoroutine(RecordBaseCoroutine(testShapeType)); }
            //call setting base value of all shapes with no value recorded on past frames
            if (callSetAllBaseAvgInfo) { callSetAllBaseAvgInfo = false; StartCoroutine(RecordAllBaseValue(false)); }
        }
        else
        {
            
        }
    }

    void Start()
    {
        //limit fps
        Application.targetFrameRate = 60;

        //initiate reord lists for avg var info
        foreach (var vi in varInfos)
        {
            vi.SetRecordList(samplingAmount);
        }

        LoadAllBaseValue();
        DisplayItemsInit();
        StartCoroutine(SamplingCoroutine());
    }

    void Update()
    {
        //execute following only if mediapipe facemesh is running
        if (s_landmarkController == null) return;

        // pass position of points to lines
        SetLine(upOutline, upOutTransform, linesIndex[0]);
        SetLine(upInline, upInTransform, linesIndex[1]);
        SetLine(bottomInline, bottomInTransform, linesIndex[2]);
        SetLine(bottomOutline, bottomOutTransform, linesIndex[3]);

        FillInfo();
        SetDisplayItemScore();
    }

    /// <summary>
    /// set position & transform reference of lines
    /// </summary>
    /// <param name="poss"></param>
    /// <param name="ts"></param>
    /// <param name="indexs"></param>
    private void SetLine(List<Vector3> poss, List<Transform> ts, List<int> indexs)
    {
        poss.Clear();
        ts.Clear();
        for (int i = 0; i < indexs.Count; i++)
        {
            try
            {
                var pos = s_landmarkController.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(indexs[i]).transform.position;
                poss.Add(pos);
                ts.Add(s_landmarkController.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(indexs[i]).transform);
            }
            catch (System.Exception e)
            {
                //Debug.Log(e.Message);
            }
        }
    }

    /// <summary>
    /// compute signed-angles of center of line towards left/right most point (from horizontal line towards left/right)
    /// </summary>
    /// <param name="poss"></param>
    /// <param name="mirror"></param>
    /// <returns></returns>
    private Tuple<float, float> GetAngles(List<Vector3> poss, bool mirror = false)
    {
        int l = poss.Count;
        if (l == 0) return null;
        return mirror ? Tuple.Create(Vector2.SignedAngle(Vector2.left, poss[0] - poss[l / 2])
            , Vector2.SignedAngle(Vector2.right, poss[l - 1] - poss[l / 2])) 
            : Tuple.Create(Vector2.SignedAngle(Vector2.right, poss[0] - poss[l / 2])
            , Vector2.SignedAngle(Vector2.left, poss[l - 1] - poss[l / 2]));
    }

    /// <summary>
    /// compute var info & shape scores
    /// </summary>
    private void FillInfo()
    {
        // execute followings only if lines are given
        if ((bottomInline.Count == 0 || upInline.Count == 0 || bottomOutline.Count == 0 || upOutline.Count == 0)) return;

        // get required information for var info
        Tuple<float, float> upOut = GetAngles(upOutline);
        Tuple<float, float> upIn = GetAngles(upInline);
        Tuple<float, float> bottomIn = GetAngles(bottomInline);
        Tuple<float, float> bottomOut = GetAngles(bottomOutline);

        LineRenderer lr = s_landmarkController.transform.GetChild(2).GetComponentInChildren<LineRenderer>();
        float angleOffset = Vector2.SignedAngle(lr.GetPosition(1) - lr.GetPosition(0), Vector2.up);
        float faceSize = Vector3.Distance(s_landmarkController.transform.TransformPoint(lr.GetPosition(0)), s_landmarkController.transform.TransformPoint(lr.GetPosition(1)));
        
        // fill var info
        c_height = Vector3.Distance(upInline[upInline.Count / 2], bottomInline[bottomInline.Count / 2]) / faceSize;
        c_width = Vector3.Distance(upInline[0], upInline[upInline.Count - 1]) / faceSize;
        c_heightWidthRatio = c_height / c_width;
        c_rightUpDeg = upOut.Item1 + angleOffset;
        c_leftUpDeg = upOut.Item2 + angleOffset;
        c_rightBottomDeg = bottomOut.Item1 + angleOffset;
        c_leftBottomDeg = bottomOut.Item2 + angleOffset;

        // compute shape scores
        foreach (var ss in shapeScores)
        {
            if (ss.isDisabled) continue;
            //if (!ss.isReady) continue;
            float totalWeight = 0;
            float totalScore = 0;
            float totalAvgScore = 0;
            foreach (var v in ss.vars)
            {
                totalWeight += v.weighting + v.weighting_ratio;
                v.varScore = (v.curve.Evaluate(GetScoreVariableInput(v.scoreVariableType) - v.baseValue) * v.weighting + v.curve_ratio.Evaluate((GetScoreVariableInput(v.scoreVariableType) - v.baseValue) / v.baseValue) * v.weighting_ratio) / (v.weighting + v.weighting_ratio);
                totalScore += v.varScore * (v.weighting + v.weighting_ratio);
                totalAvgScore += v.curve.Evaluate(GetAvgScoreVariableInput(v.scoreVariableType) - v.baseValue) * v.weighting + v.curve_ratio.Evaluate((GetScoreVariableInput(v.scoreVariableType) - v.baseValue) / v.baseValue) * v.weighting_ratio;
            }
            
            ss.score = totalScore / totalWeight;
            ss.avgScoreOvertime = totalAvgScore / totalWeight;
            //ss.AddScoreRecord(ss.score);
        }
    }

    /// <summary>
    /// coroutine for computing avg score
    /// </summary>
    /// <returns></returns>
    private IEnumerator SamplingCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1 / samplingFrequency);
            foreach (var vi in varInfos)
            {
                vi.AddInfoRecord(GetScoreVariableInput(vi.variableType));
            }
        }
    }

    /// <summary>
    /// coroutine for computing avg score for base value
    /// </summary>
    /// <param name="shapeType"></param>
    /// <returns></returns>
    public IEnumerator RecordBaseCoroutine(ShapeType shapeType)
    {
        Debug.Log("ready to record in" + baseValueReadyDuration + "sec: " + shapeType.ToString());
        yield return new WaitForSeconds(baseValueReadyDuration);
        Debug.Log("recording...");
        foreach (var bvi in baseVarInfos)
        {
            bvi.SetRecordList(baseValueSamplingAmount);
        }
        while (baseVarInfos.Any(x => !x.isCycled))
        {
            foreach (var bvi in baseVarInfos)
            {
                bvi.AddInfoRecord(GetScoreVariableInput(bvi.variableType));
            }
            yield return new WaitForSeconds(1 / samplingFrequency);
        }
        SetBaseValue(shapeType, isAvg: true);
        Debug.Log("record done");
    }

    /// <summary>
    /// coroutine to call RecordBseCoroutine of all shapes, (skipping shapes having record in playerprefs)
    /// </summary>
    /// <param name="isSkipRecord"></param>
    /// <returns></returns>
    public IEnumerator RecordAllBaseValue(bool isSkipRecord)
    {
        foreach (var ss in shapeScores)
        {
            if (isSkipRecord && !PlayerPrefs.HasKey("baseValue_" + ss.shapeType.ToString())) continue;
            yield return RecordBaseCoroutine(ss.shapeType);
        }
        Debug.Log("base values of all shape types had been saved");
    }

    /// <summary>
    /// load all base values from playerprefs
    /// </summary>
    private void LoadAllBaseValue()
    {
        foreach (var ss in shapeScores)
        {
            if (!PlayerPrefs.HasKey("baseValue_" + ss.shapeType.ToString())) continue;
            var lis = JsonUtility.FromJson<SaveDataList>(PlayerPrefs.GetString("baseValue_" + ss.shapeType.ToString()));
            foreach (var v in ss.vars)
            {
                SaveData saveData = lis.dataList.Find(x => x.key == v.scoreVariableType);
                if (saveData == null) continue;
                v.baseValue = saveData.value;
            }
        }
    }

    /// <summary>
    /// set base value of shape, (from current value / avg value)
    /// </summary>
    /// <param name="shapeType"></param>
    /// <param name="isAvg"></param>
    private void SetBaseValue(ShapeType shapeType, bool isAvg = false)
    {
        FillInfo();
        ShapeScore ss = shapeScores.First((x) => x.shapeType == shapeType);
        List<SaveData> lis = new();
        foreach (var v in ss.vars)
        {
            v.baseValue = isAvg ? baseVarInfos.First(x => x.variableType == v.scoreVariableType).avgVarInfoOvertime : GetScoreVariableInput(v.scoreVariableType);
            //dic[v.scoreVariableType] = v.baseValue;
            lis.Add(new SaveData(v.scoreVariableType, v.baseValue));
        }
        PlayerPrefs.SetString("baseValue_" + shapeType.ToString(), JsonUtility.ToJson(new SaveDataList(lis)));
        Debug.Log("saved base value of shape type (" + shapeType.ToString() + ") with " + (isAvg ? "average recorded" : "current") + " value");
    }

    #region functions providing data access for others
    /// <summary>
    /// access to score representing how likely the current lip is the shapeType
    /// </summary>
    /// <param name="shapeType">specific ShapeType needed</param>
    /// <returns>score representing how likely the current lip is the shapeType [-1,1]</returns>
    public float GetShapeCurrentScore(ShapeType shapeType) { return shapeScores.First((x) => x.shapeType == shapeType).score; }

    /// <summary>
    /// access to avg score representing how likely the past lip is the shapeType
    /// </summary>
    /// <param name="shapeType">specific ShapeType needed</param>
    /// <returns>avg score representing how likely the past lip is the shapeType [-1,1]</returns>
    public float GetShapeAvgScore(ShapeType shapeType) { return shapeScores.First((x) => x.shapeType == shapeType).avgScoreOvertime; }

    /// <summary>
    /// access to list of all scores representing how likely the current lip is each shapeType
    /// </summary>
    /// <param name="shapeType">specific ShapeType needed</param>
    /// <returns>score representing how likely the current lip is the shapeType [-1,1]</returns>
    public List<Tuple<ShapeType, float>> GetAllShapeCurrentScore() 
    {
        List<Tuple<ShapeType, float>> result = new();
        Array.ForEach(shapeScores, (x) => { result.Add(new Tuple<ShapeType, float>(x.shapeType, x.score)); });
        return result;
    }

    /// <summary>
    /// access to list of all avg scores representing how likely the past lip is each shapeType
    /// </summary>
    /// <param name="shapeType">specific ShapeType needed</param>
    /// <returns>avg score representing how likely the past lip is the shapeType [-1,1]</returns>
    public List<Tuple<ShapeType, float>> GetAllShapeAvgScore()
    {
        List<Tuple<ShapeType, float>> result = new();
        Array.ForEach(shapeScores, (x) => { result.Add(new Tuple<ShapeType, float>(x.shapeType, x.avgScoreOvertime)); });
        return result;
    }

    /// <summary>
    /// access to value of score variable with type of scoreVariableType
    /// </summary>
    /// <param name="scoreVariableType">specific ScoreVariableType needed</param>
    /// <returns>value of score variable with type of scoreVariableType</returns>
    public float GetScoreVariableInput(ScoreVariableType scoreVariableType)
    {
        switch (scoreVariableType)
        {
            case ScoreVariableType.c_rightUpDeg:
                return c_rightUpDeg;
            case ScoreVariableType.c_leftUpDeg:
                return c_leftUpDeg;
            case ScoreVariableType.c_rightBottomDeg:
                return c_rightBottomDeg;
            case ScoreVariableType.c_leftBottomDeg:
                return c_leftBottomDeg;
            case ScoreVariableType.c_height:
                return c_height;
            case ScoreVariableType.c_width:
                return c_width;
            case ScoreVariableType.c_heightWidthRatio:
                return c_heightWidthRatio;
            default:
                return float.NaN;
        }
    }

    /// <summary>
    /// access to avg value of score variable with type of scoreVariableType
    /// </summary>
    /// <param name="scoreVariableType">specific ScoreVariableType needed</param>
    /// <returns>avg value of score variable with type of scoreVariableType</returns>
    public float GetAvgScoreVariableInput(ScoreVariableType scoreVariableType)
    {
        return varInfos.First(x => x.variableType == scoreVariableType).avgVarInfoOvertime;
    }

    #endregion

    #region custom classes & enums
    [Serializable] public class ShapeScore
    {
        [Tooltip("shape of lip considered")] public ShapeType shapeType;
        [Tooltip("score")] public float score;
        [Tooltip("avg score")] public float avgScoreOvertime;
        [Tooltip("variables considered")] public ScoreVariable[] vars;
        [Tooltip("control whether avoid this from being processed")] public bool isDisabled;
    }
    [Serializable] public class ScoreVariable
    {
        [Tooltip("type of variable considered")] public ScoreVariableType scoreVariableType;
        [Tooltip("baseline value")] public float baseValue;
        [Tooltip("weighting of how significant of differece between varScore & baseValue to be considered when computing score of shape")] public float weighting;
        [Tooltip("curve computing score from differece between varScore & baseValue to be considered when computing score of shape")] public AnimationCurve curve;
        [Tooltip("weighting of how significant of ratio differece between varScore & baseValue [(varScore - baseValue) / baseValue] to be considered when computing score of shape")] public float weighting_ratio;
        [Tooltip("curve computing score from ratio differece between varScore & baseValue [(varScore - baseValue) / baseValue] to be considered when computing score of shape")] public AnimationCurve curve_ratio;
        [Tooltip("avg value")] public float varScore;
    }
    public enum ScoreVariableType
    {
        c_rightUpDeg,
        c_leftUpDeg,
        c_rightBottomDeg,
        c_leftBottomDeg,
        c_height,
        c_width,
        c_heightWidthRatio,
    }
    public enum ShapeType
    {
        None,
        smiling,
        notSmiling,
        a,
        i,
        u,
        rest,
    }
    [Serializable] public class VarInfo
    {
        [Tooltip("type of variable")] public ScoreVariableType variableType;
        [Tooltip("average value of variable in past frames")] public float avgVarInfoOvertime;
        private List<float> infoRecord = new();
        private int currentRecordIndex = 0;
        [Tooltip("indicate whether number of record is enough for computing average value")] public bool isCycled = false;
        public void AddInfoRecord(float s)
        {
            infoRecord[currentRecordIndex] = s;
            currentRecordIndex = (currentRecordIndex + 1) % infoRecord.Count;
            avgVarInfoOvertime = infoRecord.Sum() / (float)infoRecord.Count;
            if (currentRecordIndex==0) isCycled = true;
        }
        public void SetRecordList(int num)
        {
            infoRecord = new List<float>(new float[num]);
            currentRecordIndex = 0;
            isCycled = false;
        }
        public VarInfo(ScoreVariableType t)
        {
            this.variableType = t;
        }
    }
    [Serializable] public class SaveData
    {
        public ScoreVariableType key;
        public float value;
        public SaveData(ScoreVariableType k, float v)
        {
            key = k;
            value = v;
        }
    }
    [Serializable] public class SaveDataList
    {
        public List<SaveData> dataList;
        public SaveDataList(List<SaveData> lis)
        {
            dataList = lis;
        }
    }
    #endregion

    #region display
    /// <summary>
    /// initialize display item in canvas
    /// </summary>
    private void DisplayItemsInit()
    {
        for (int i=0; i < shapeScores.Count(); i++) 
        {
            ShapeType st = shapeScores[i].shapeType;
            t_displayItemList.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = st.ToString();
            t_displayItemList.GetChild(i).GetChild(2).GetComponent<Button>().onClick.AddListener(() => { SetBaseValue(st); });
            t_displayItemList.GetChild(i).GetChild(3).GetComponent<Button>().onClick.AddListener(() => { StartCoroutine(RecordBaseCoroutine(st)); });
        }
    }

    /// <summary>
    /// set score in display item texts
    /// </summary>
    private void SetDisplayItemScore()
    {
        float maxAvgScore = 0f;
        for (int i = 0; i < shapeScores.Count(); i++)
        {
            if (shapeScores[i].isDisabled)
            {
                t_displayItemList.GetChild(i).gameObject.SetActive(false);
                continue;
            }
            t_displayItemList.GetChild(i).gameObject.SetActive(true);
            t_displayItemList.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text 
                = ((shapeScores[i].score > threashold) ? "<color=green>" + shapeScores[i].score.ToString("f" + 2) + "</color>" : shapeScores[i].score.ToString("f" + 2))
                + "|" 
                + ((shapeScores[i].avgScoreOvertime > threashold) ? "<color=green>" + shapeScores[i].avgScoreOvertime.ToString("f" + 2) + "</color>" : shapeScores[i].avgScoreOvertime.ToString("f" + 2));
            maxAvgScore = Mathf.Max(maxAvgScore, shapeScores[i].avgScoreOvertime);
        }
        for (int i = 0; i < shapeScores.Count(); i++)
        {
            if (maxAvgScore - shapeScores[i].avgScoreOvertime <= maxRange)
            {
                t_displayItemList.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = "<color=blue>" + shapeScores[i].shapeType.ToString() + "</color>";
            }
            else
            {
                t_displayItemList.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = shapeScores[i].shapeType.ToString();
            }
        }
    }
    #endregion

    /* old test function
    // compute display text of given floats
    private string FormatFloatTuple(Tuple<float, float> t, int sigFig)
    {
        if (t == null) return "";
        return "(" + t.Item1.ToString("f" + sigFig) + "/" + t.Item2.ToString("f" + sigFig) + ")";
    }

    private void FillInfoText()
    {
        string result = "";
        Tuple<float, float> uo = GetAngles(upOutline);
        Tuple<float, float> ui = GetAngles(upInline);
        result += "up out deg: " + FormatFloatTuple(uo, 2) + '\n';
        result += "Smiling(?): " + ((uo == null) ? "" : 
            (rightSlopes[0].Evaluate(uo.Item1) 
            + leftSlopes[0].Evaluate(uo.Item2)
            + rightSlopes[1].Evaluate(ui.Item1)
            + leftSlopes[1].Evaluate(ui.Item2))) 
            + '\n';
        result += "Opened: " + ((bottomInline.Count == 0 || upInline.Count == 0 || bottomOutline.Count == 0 || upOutline.Count == 0) ? "" : (Mathf.Abs(bottomInline[5].y - upInline[5].y) / Mathf.Abs(upInline[5].y - upOutline[5].y)));
        infoText.text = result;
    }
    */
}
