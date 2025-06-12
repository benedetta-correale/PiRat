using UnityEngine;
using System.Collections.Generic;

public class PossessionManager : MonoBehaviour
{
    [Header("Riferimenti")]
    public RatInteractionManager ratInteraction;
    public GameObject sciaPrefab;
    public Transform ratTransform;

    [Header("Impostazioni selezione")]
    public bool isSelecting = false;
    private int selectedIndex = -1;

    private List<Transform> InfectedPirates => ratInteraction.infectedPirates;
    private List<LineRenderer> scieAttive = new List<LineRenderer>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            EnterSelectionMode();
        }

        if (!isSelecting || InfectedPirates.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitSelectionMode();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmSelection();
        }

        Vector2 inputDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (inputDir != Vector2.zero)
        {
            SelectClosestInDirection(inputDir.normalized);
        }

        AggiornaScie();
    }

    void EnterSelectionMode()
    {
        isSelecting = true;
        selectedIndex = -1;
        ShowScie();
    }

    void ExitSelectionMode()
    {
        isSelecting = false;
        selectedIndex = -1;
        HideScie();
    }

    void ConfirmSelection()
    {
        if (selectedIndex >= 0 && selectedIndex < InfectedPirates.Count)
        {
            CameraControlManager.Instance.SwitchToPirate(InfectedPirates[selectedIndex]);
            ExitSelectionMode();
        }
    }

    void SelectClosestInDirection(Vector2 inputDir)
    {
        float bestDot = -1f;
        int bestIndex = -1;

        for (int i = 0; i < InfectedPirates.Count; i++)
        {
            Vector3 toPirate = InfectedPirates[i].position - ratTransform.position;
            Vector2 toPirate2D = new Vector2(toPirate.x, toPirate.z).normalized;
            float dot = Vector2.Dot(inputDir, toPirate2D);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestIndex = i;
            }
        }

        if (bestIndex != -1)
        {
            selectedIndex = bestIndex;
            Debug.Log("Pirata selezionato: " + InfectedPirates[selectedIndex].name);
        }
    }

    void AggiornaScie()
    {
        if (!isSelecting) return;

        var infected = InfectedPirates;

        while (scieAttive.Count < infected.Count)
        {
            var newScia = Instantiate(sciaPrefab).GetComponent<LineRenderer>();
            newScia.gameObject.SetActive(false);
            scieAttive.Add(newScia);
        }

        while (scieAttive.Count > infected.Count)
        {
            Destroy(scieAttive[scieAttive.Count - 1].gameObject);
            scieAttive.RemoveAt(scieAttive.Count - 1);
        }

        for (int i = 0; i < infected.Count; i++)
        {
            var scia = scieAttive[i];
            var target = infected[i];

            scia.SetPosition(0, ratTransform.position);
            scia.SetPosition(1, target.position + Vector3.up * 0.5f);
        }
    }

    public void ShowScie()
    {
        foreach (var scia in scieAttive)
        {
            scia.gameObject.SetActive(true);
        }
    }

    public void HideScie()
    {
        foreach (var scia in scieAttive)
        {
            scia.gameObject.SetActive(false);
        }
    }
}

