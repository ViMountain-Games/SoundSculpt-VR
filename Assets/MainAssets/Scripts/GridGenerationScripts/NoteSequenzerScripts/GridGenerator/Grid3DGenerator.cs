using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomInspector;
using TMPro; // Per TextMeshPro
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Events;
using Autohand;

namespace GridGen
{
    [DefaultExecutionOrder(-100)]
    public class Grid3DGenerator : MonoBehaviour
    {
        public enum NoteName
        {
            Do,
            Re,
            Mi,
            Fa,
            Sol,
            La,
            Si,
            DoSharp,
            ReSharp,
            FaSharp,
            SolSharp,
            LaSharp,
            ReFlat,
            MiFlat,
            SolFlat,
            LaFlat,
            SiFlat
        }

        [System.Serializable]
        public class GridEntry
        {
            public GameObject gameObject;
            public int x, y, z;

            public GridEntry(GameObject gameObject, int x, int y, int z)
            {
                this.gameObject = gameObject;
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }

        [System.Serializable]
        public class OctaveMapping
        {
            public int zValue;
            public int octave;
        }

        [System.Serializable]
        public class NoteMapping
        {
            public int yValue;
            public NoteName noteName;
        }

        [System.Serializable]
        public class LabelSettings
        {
            public float offsetX = -0.5f;
            public float textSize = 1f;
            public Color textColor = Color.white;
        }

        [Title("Grid Settings", fontSize = 14, alignment = TextAlignment.Center)]
        [Min(1)] public int gridSizeX = 5;
        [Min(1)] public int gridSizeY = 17;
        [Min(1)] public int gridSizeZ = 3;
        [DynamicSlider]
        public DynamicSlider cellSize = new DynamicSlider(1f, 0.1f, 5f);

        [HorizontalLine("Prefabs and Materials", 2)]
        [ForceFill] public GameObject cellPrefab;
        [ForceFill] public Material lineMaterial;
        [ForceFill] public GameObject labelPrefab;

        [Header("Label Settings")]
        public LabelSettings labelSettings = new LabelSettings();

        [HorizontalLine("Visual Settings", 2)]
        [ColorPalette] public Color emptyCellColor = Color.gray;
        [ColorPalette] public Color lineColor = Color.white;
        [Range(0.001f, 0.5f)] public float lineWidth = 0.05f;

        [HorizontalLine("Bar Line Settings", 2)]
        [Min(1)] public int cellsPerBar = 4;
        [ForceFill] public GameObject barLinePrefab;
        public float yOffset = 0f;
        public float zOffset = 0f;

        [HorizontalLine("Animation Settings", 2)]
        [Header("Animation Settings")]
        public float lineAnimationDuration = 0.5f;
        public float lineAnimationDelay = 0.05f;
        public float cellInstantiationDelay = 0.1f;

        [HorizontalLine("Grid Matrix", 2)]
        [ReadOnly]
        public GameObject[,,] gridMatrix;

        [HorizontalLine("Object List", 2)]
        [ReadOnly]
        public List<GridEntry> objectList = new List<GridEntry>();

        [Header("Octave Mappings")]
        [SerializeField]
        public List<OctaveMapping> octaveMappings = new List<OctaveMapping>();

        [Header("Default Octave Settings")]
        [Min(1)] public int defaultOctave = 2;

        [Header("Note Mappings")]
        [SerializeField]
        public List<NoteMapping> noteMappings = new List<NoteMapping>();

        [Header("Auto Generate Settings")]
        public bool generateOnStart = false;

        [SerializeField]
        private MeshRendererActivatorManager meshActivatorManager;

        [Title("Solution Grid Configuration (NoteData)")]
        [Tooltip("Array monodimensionale per gestire la solution grid")]
        [SerializeField]
        private NoteData[] solutionCells; // = gridSizeX * gridSizeY * gridSizeZ

        [HorizontalLine("Combination Events", 2)]
        public UnityEvent OnCorrectCombination;
        public UnityEvent OnIncorrectCombination;

        [HorizontalLine("Grid Events", 2)]
        public UnityEvent OnGridGenerated;
        public UnityEvent OnGridCleared;

        [ReadOnly]
        public int attempts = 0;

        private GameObject labelsParent;
        private List<LineData> lineDataList = new List<LineData>();

        private void Awake()
        {
            if (MeshRendererActivatorManager.Instance != null)
                meshActivatorManager = MeshRendererActivatorManager.Instance;

            if (generateOnStart)
            {
                GenerateGrid();
            }
        }

        private void OnValidate()
        {
            EnsureSolutionArraySize();
        }

        private void EnsureSolutionArraySize()
        {
            int neededSize = gridSizeX * gridSizeY * gridSizeZ;
            if (solutionCells == null || solutionCells.Length != neededSize)
            {
                NoteData[] newArray = new NoteData[neededSize];
                if (solutionCells != null)
                {
                    int minSize = Mathf.Min(neededSize, solutionCells.Length);
                    for (int i = 0; i < minSize; i++)
                        newArray[i] = solutionCells[i];
                }
                solutionCells = newArray;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public void GenerateGrid()
        {
            ClearGrid();

            gridMatrix = new GameObject[gridSizeX, gridSizeY, gridSizeZ];

            GameObject gridParent = new GameObject("3DGrid")
            {
                transform = { parent = this.transform }
            };
            labelsParent = new GameObject("Labels")
            {
                transform = { parent = this.transform }
            };

            Vector3 origin = transform.position;
            float gridDepth = gridSizeZ * cellSize.value;

            // Generazione label
            for (int y = 0; y < gridSizeY; y++)
            {
                NoteName noteName = GetNoteNameFromY(y);
                Vector3 labelPosition = origin + new Vector3(
                    labelSettings.offsetX * cellSize.value,
                    y * cellSize.value + cellSize.value / 2,
                    gridDepth / 2
                );

                GameObject labelInstance = Instantiate(labelPrefab, labelPosition, Quaternion.identity, labelsParent.transform);
                TextMeshPro tmp = labelInstance.GetComponentInChildren<TextMeshPro>();
                if (tmp != null)
                {
                    tmp.text = FormatNoteName(noteName);
                    tmp.fontSize = labelSettings.textSize;
                    tmp.color = labelSettings.textColor;
                }
                else
                {
                    Debug.LogError("[Grid3DGenerator] Label prefab missing TextMeshPro child.");
                }
            }

            CreateGridLines(origin, gridParent);
            StartCoroutine(AnimateGridLinesAndInstantiateCells());
            GenerateBarLines(origin);

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif

            Debug.Log("Grid generated successfully.");
            OnGridGenerated?.Invoke();
        }

        public void ClearGrid()
        {
            StopAllCoroutines();

            if (labelsParent != null)
            {
                Destroy(labelsParent);
                labelsParent = null;
            }

            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }

            foreach (GameObject child in children)
            {
                Destroy(child);
            }

            gridMatrix = null;
            objectList.Clear();
            lineDataList.Clear();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif

            Debug.Log("Grid cleared successfully.");
            OnGridCleared?.Invoke();
        }

        private void CreateGridLines(Vector3 origin, GameObject parent)
        {
            for (int x = 0; x <= gridSizeX; x++)
            {
                for (int y = 0; y <= gridSizeY; y++)
                {
                    Vector3 start = origin + new Vector3(x * cellSize.value, y * cellSize.value, 0);
                    Vector3 end = origin + new Vector3(x * cellSize.value, y * cellSize.value, gridSizeZ * cellSize.value);
                    lineDataList.Add(new LineData(start, end, parent, lineMaterial, Color.white, lineWidth));
                }
            }

            for (int y = 0; y <= gridSizeY; y++)
            {
                for (int z = 0; z <= gridSizeZ; z++)
                {
                    Vector3 start = origin + new Vector3(0, y * cellSize.value, z * cellSize.value);
                    Vector3 end = origin + new Vector3(gridSizeX * cellSize.value, y * cellSize.value, z * cellSize.value);
                    lineDataList.Add(new LineData(start, end, parent, lineMaterial, Color.white, lineWidth));
                }
            }

            for (int x = 0; x <= gridSizeX; x++)
            {
                for (int z = 0; z <= gridSizeZ; z++)
                {
                    Vector3 start = origin + new Vector3(x * cellSize.value, 0, z * cellSize.value);
                    Vector3 end = origin + new Vector3(x * cellSize.value, gridSizeY * cellSize.value, z * cellSize.value);
                    lineDataList.Add(new LineData(start, end, parent, lineMaterial, Color.white, lineWidth));
                }
            }
        }

        private IEnumerator AnimateGridLinesAndInstantiateCells()
        {
            for (int i = 0; i < lineDataList.Count; i++)
            {
                var lineData = lineDataList[i];
                lineData.lineRenderer.enabled = true;
                StartCoroutine(AnimateLine(lineData.lineRenderer, lineData.start, lineData.end));
                yield return new WaitForSeconds(lineAnimationDelay);
            }

            yield return new WaitForSeconds(lineAnimationDuration);
            yield return StartCoroutine(InstantiateCells());
        }

        private IEnumerator AnimateLine(LineRenderer lineRenderer, Vector3 start, Vector3 end)
        {
            float elapsedTime = 0f;
            while (elapsedTime < lineAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / lineAnimationDuration;
                Vector3 currentPos = Vector3.Lerp(start, end, t);
                lineRenderer.SetPosition(1, currentPos);
                yield return null;
            }
            lineRenderer.SetPosition(1, end);
        }

        private IEnumerator InstantiateCells()
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        Vector3 cellCenter = transform.position + new Vector3(
                            x * cellSize.value + cellSize.value / 2,
                            y * cellSize.value + cellSize.value / 2,
                            z * cellSize.value + cellSize.value / 2
                        );

                        GameObject newCell = Instantiate(cellPrefab, cellCenter, Quaternion.identity, transform);

                        // [MODIFICA IMPORTANTE] Assegniamo 'this' a tutti i componenti nel cellPrefab
                        NotePicker picker = newCell.GetComponent<NotePicker>();
                        if (picker != null)
                        {
                            picker.gridGenerator = this;
                        }

                        // Se c'è un MusicScaleGenerator nel cell prefab, assegna pure lì
                        MusicScaleGenerator musicScale = newCell.GetComponent<MusicScaleGenerator>();
                        if (musicScale != null)
                        {
                            musicScale.gridGenerator = this;
                        }

                        if (meshActivatorManager != null)
                            meshActivatorManager.RegisterCell(newCell);

                        gridMatrix[x, y, z] = null;
                        yield return new WaitForSeconds(cellInstantiationDelay);
                    }
                }
            }
        }

        private void GenerateBarLines(Vector3 origin)
        {
            if (cellsPerBar < 1)
            {
                Debug.LogError("cellsPerBar deve essere almeno 1.");
                return;
            }

            GameObject barLinesParent = new GameObject("BarLines") { transform = { parent = this.transform } };
            for (int x = cellsPerBar; x < gridSizeX; x += cellsPerBar)
            {
                Vector3 barLinePosition = origin + new Vector3(
                    x * cellSize.value,
                    gridSizeY * cellSize.value / 2,
                    gridSizeZ * cellSize.value / 2
                );

                GameObject barLineInstance = Instantiate(barLinePrefab, barLinePosition, Quaternion.identity, barLinesParent.transform);
                Vector3 barLineScale = barLineInstance.transform.localScale;
                barLineScale.y = gridSizeY * cellSize.value + yOffset;
                barLineScale.z = gridSizeZ * cellSize.value + zOffset;
                barLineInstance.transform.localScale = barLineScale;
            }
        }

        public string FormatNoteName(NoteName noteName)
        {
            switch (noteName)
            {
                case NoteName.DoSharp: return "Do♯";
                case NoteName.ReSharp: return "Re♯";
                case NoteName.FaSharp: return "Fa♯";
                case NoteName.SolSharp: return "Sol♯";
                case NoteName.LaSharp: return "La♯";
                case NoteName.ReFlat:  return "Reb";
                case NoteName.MiFlat:  return "Mib";
                case NoteName.SolFlat: return "Solb";
                case NoteName.LaFlat:  return "Lab";
                case NoteName.SiFlat:  return "Sib";
                default: return noteName.ToString();
            }
        }

        public int GetOctaveFromZ(int z)
        {
            foreach (var mapping in octaveMappings)
            {
                if (mapping.zValue == z) return mapping.octave;
            }
            Debug.LogWarning($"[Grid3DGenerator] No octave mapping for z={z}, using default {defaultOctave}.");
            return defaultOctave;
        }

        public NoteName GetNoteNameFromY(int y)
        {
            foreach (var mapping in noteMappings)
            {
                if (mapping.yValue == y) return mapping.noteName;
            }
            Debug.LogWarning($"[Grid3DGenerator] No note mapping for y={y}, using 'Do'.");
            return NoteName.Do;
        }

        public void UpdateGridMatrix(int x, int y, int z, GameObject newObject)
        {
            if (gridMatrix == null)
            {
                Debug.LogError("GridMatrix non è inizializzata.");
                return;
            }
            if (x < 0 || x >= gridSizeX || y < 0 || y >= gridSizeY || z < 0 || z >= gridSizeZ)
            {
                Debug.LogError($"Indici non validi: ({x},{y},{z}).");
                return;
            }
            gridMatrix[x, y, z] = newObject;
        }

        public bool CanPlaceNote(int x, int y, int z, NoteData.NoteDuration duration)
        {
            int length = (duration == NoteData.NoteDuration.Quarter) ? 1 :
                         (duration == NoteData.NoteDuration.Half) ? 2 : 4;
            for (int i = 0; i < length; i++)
            {
                int checkX = x + i;
                if (checkX < 0 || checkX >= gridSizeX || y < 0 || y >= gridSizeY || z < 0 || z >= gridSizeZ)
                    return false;
                if (gridMatrix[checkX, y, z] != null)
                    return false;
            }
            return true;
        }

        public void PlaceNoteInGrid(int x, int y, int z, GameObject noteObj, NoteData.NoteDuration duration)
        {
            int length = (duration == NoteData.NoteDuration.Quarter) ? 1 :
                         (duration == NoteData.NoteDuration.Half) ? 2 : 4;

            for (int i = 0; i < length; i++)
            {
                int placeX = x + i;
                if (placeX >= 0 && placeX < gridSizeX &&
                    y >= 0 && y < gridSizeY &&
                    z >= 0 && z < gridSizeZ)
                {
                    gridMatrix[placeX, y, z] = noteObj;
                }
            }
        }

        public void RemoveNoteFromGrid(int x, int y, int z, NoteData.NoteDuration duration)
        {
            int length = (duration == NoteData.NoteDuration.Quarter) ? 1 :
                         (duration == NoteData.NoteDuration.Half) ? 2 : 4;
            for (int i = 0; i < length; i++)
            {
                int remX = x + i;
                if (remX >= 0 && remX < gridSizeX &&
                    y >= 0 && y < gridSizeY &&
                    z >= 0 && z < gridSizeZ)
                {
                    gridMatrix[remX, y, z] = null;
                }
            }
        }

        public void IncreaseAttempts() => attempts++;
        public void DecreaseAttempts() { if (attempts > 0) attempts--; }

        public void CheckCombination()
        {
            if (gridMatrix == null || solutionCells == null)
            {
                Debug.LogError("[Grid3DGenerator] GridMatrix or solutionCells is null - can't check combination.");
                return;
            }

            bool isCorrect = true;
            for (int x = 0; x < gridSizeX && isCorrect; x++)
            {
                for (int y = 0; y < gridSizeY && isCorrect; y++)
                {
                    for (int z = 0; z < gridSizeZ && isCorrect; z++)
                    {
                        int index = GetIndex(x, y, z);
                        NoteData expectedNote = solutionCells[index];
                        GameObject placedObj = gridMatrix[x, y, z];

                        if (expectedNote == null)
                        {
                            if (placedObj != null) isCorrect = false;
                        }
                        else
                        {
                            if (placedObj == null) isCorrect = false;
                            else
                            {
                                Note placedNote = placedObj.GetComponent<Note>();
                                if (placedNote == null || placedNote.noteData == null)
                                    isCorrect = false;
                                else
                                    if (placedNote.noteData != expectedNote)
                                        isCorrect = false;
                            }
                        }
                    }
                }
            }

            if (isCorrect) OnCorrectCombination?.Invoke();
            else OnIncorrectCombination?.Invoke();
        }

        public int GetIndex(int x, int y, int z)
        {
            return x + gridSizeX * (y + gridSizeY * z);
        }

        public void SetSolutionCell(int x, int y, int z, NoteData note)
        {
            int index = GetIndex(x, y, z);
            solutionCells[index] = note;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public NoteData GetSolutionCell(int x, int y, int z)
        {
            int index = GetIndex(x, y, z);
            return solutionCells[index];
        }

        private class LineData
        {
            public Vector3 start;
            public Vector3 end;
            public LineRenderer lineRenderer;

            public LineData(Vector3 start, Vector3 end, GameObject parent, Material material, Color color, float width)
            {
                this.start = start;
                this.end = end;
                GameObject lineObject = new GameObject("Line");
                lineObject.transform.parent = parent.transform;

                lineRenderer = lineObject.AddComponent<LineRenderer>();
                lineRenderer.material = material;
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
                lineRenderer.startWidth = width;
                lineRenderer.endWidth = width;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, start);
                lineRenderer.enabled = false;
            }
        }
    }
}
