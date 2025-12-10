using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class WiggleEachCharOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float wiggleAmplitude = 2f;    // Wiggle height per char
    [SerializeField] private float wiggleFrequency = 6f;     // Wiggle speed
    [SerializeField] private float charOffset = 0.25f;       // Phase offset between chars

    private TextMeshProUGUI tmpText;
    private TMP_TextInfo textInfo;
    private bool isHovering = false;
    private float time = 0f;

    private void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        tmpText.ForceMeshUpdate();
        textInfo = tmpText.textInfo;
    }

    private void Update()
    {
        if (!isHovering) return;

        time += Time.deltaTime;

        tmpText.ForceMeshUpdate();
        textInfo = tmpText.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;

            Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;

            // Offset each character with phase shift
            float offsetY = Mathf.Sin(time * wiggleFrequency + i * charOffset) * wiggleAmplitude;

            Vector3 offset = new Vector3(0f, offsetY, 0f);
            verts[vertexIndex + 0] += offset;
            verts[vertexIndex + 1] += offset;
            verts[vertexIndex + 2] += offset;
            verts[vertexIndex + 3] += offset;
        }

        // Apply mesh changes
        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            TMP_MeshInfo meshInfo = textInfo.meshInfo[m];
            meshInfo.mesh.vertices = meshInfo.vertices;
            tmpText.UpdateGeometry(meshInfo.mesh, m);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        time = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        // Reset text so characters don’t freeze offset
        tmpText.ForceMeshUpdate();
    }
}
