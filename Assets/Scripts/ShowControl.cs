// ============================================================================
//  ShowControl.cs  —  Eén tijdlijn voor je hele show, over ALLE Nodes heen.
//
//  WAT IS DIT:
//   Zet dit script op één (leeg) GameObject in de scene. Daarna kun je in
//   Start() (of vanuit eender welk script) een hele reeks acties inplannen,
//   elk met een eigen tijdstip en een eigen doel-Node:
//
//      Event.ZetServo(2, 3, 45);   // na 2 s: Node 3, servo naar 45°
//      Event.ZetServo(5, 1, 80);   // na 5 s: Node 1, servo naar 80°
//      Event.SocketAan(3, 2);      // na 3 s: Node 2, stekkerdoos AAN
//
//   Elke regel plant zijn eigen actie in — de tijden zijn onafhankelijk van
//   elkaar (dus GEEN "wacht op de vorige stap"), gewoon "op tijdstip X vanaf
//   het moment dat Start() draait".
//
//  HOE GEBRUIK JE HET:
//   1) Maak één (leeg) GameObject en sleep dit ShowControl-script erop.
//      (er mag er maar ÉÉN in de scene staan — via 'Event' kun je hem overal
//      aanspreken zonder een referentie te moeten slepen)
//   2) De SerialController wordt automatisch gevonden (of sleep hem zelf in
//      het veld 'Verbinding').
//   3) Typ je tijdlijn in Start() als 'Event.<methode>(...)', of roep de
//      methodes vanuit een eigen script aan.
//
//  Werkt samen met SerialController (V003-protocol).
// ============================================================================
using System.Collections;
using UnityEngine;

public class ShowControl : MonoBehaviour
{
    /// Wereldwijde toegang: vanuit eender welk script/methode gebruik je "Event.ZetServo(...)".
    public static ShowControl Event { get; private set; }

    [Header("Verbinding (leeg laten = zelf zoeken)")]
    [Tooltip("De SerialController in de scene. Laat dit leeg; dan zoekt ShowControl hem automatisch.")]
    public SerialController verbinding;

    void Awake()
    {
        if (Event != null && Event != this)
        {
            Debug.LogWarning("[ShowControl] Er staat al een ShowControl in de scene — deze extra wordt genegeerd.", this);
            Destroy(this);
            return;
        }
        Event = this;
    }

    void OnEnable()
    {
        if (verbinding == null) verbinding = FindFirstObjectByType<SerialController>();
        if (verbinding == null)
            Debug.LogError("[ShowControl] Geen SerialController in de scene gevonden. " +
                           "Zet er één in de scene, of sleep hem in het veld 'Verbinding'.", this);
    }

    void OnDestroy()
    {
        if (Event == this) Event = null;
    }

    // ════════════════ Tijdlijn-acties — plan hier je show mee in ════════════════

    /// Zet na 'tijd' seconden, op 'node', de servo op een hoek tussen 0 en 180 graden.
    public void ZetServo(float tijd, int node, int hoek)
    {
        if (hoek < 0 || hoek > 180)
        {
            Debug.LogWarning($"[ShowControl] Servohoek {hoek} valt buiten 0..180 — ik kort hem af.", this);
            hoek = Mathf.Clamp(hoek, 0, 180);
        }
        Plan(tijd, node, addr => verbinding.SendServo(addr, hoek));
    }

    /// Zet na 'tijd' seconden, op 'node', de stekkerdoos (socket) AAN.
    public void SocketAan(float tijd, int node)
    {
        Plan(tijd, node, addr => verbinding.SendSocket(addr, true));
    }

    /// Zet na 'tijd' seconden, op 'node', de stekkerdoos (socket) UIT.
    public void SocketUit(float tijd, int node)
    {
        Plan(tijd, node, addr => verbinding.SendSocket(addr, false));
    }

    /// Zet na 'tijd' seconden, op 'node', een uitgang (pin 0..4) hoog (aan = true) of laag (aan = false).
    public void ZetPin(float tijd, int node, int pin, bool aan)
    {
        if (pin < 0 || pin > 4)
        {
            Debug.LogWarning($"[ShowControl] Pin {pin} bestaat niet (gebruik 0 t/m 4) — niets ingepland.", this);
            return;
        }
        Plan(tijd, node, addr => verbinding.SendPin(addr, pin, aan));
    }

    /// Laat na 'tijd' seconden, op 'node', de Node zich identificeren (LEDs knipperen even).
    public void StuurIdent(float tijd, int node)
    {
        Plan(tijd, node, addr => verbinding.Ident(addr));
    }

    // ───────────────────────────── kleine helpers ─────────────────────────────

    // Plan één actie in: wacht 'tijd' seconden, controleer dan de verbinding en het
    // Node-adres, en voer 'actie' pas op dat moment uit.
    void Plan(float tijd, int node, System.Action<byte> actie)
    {
        if (!HeeftVerbinding()) return;
        byte addr = (byte)Mathf.Clamp(node, 1, 63);
        StartCoroutine(WachtEnVoerUit(Mathf.Max(0f, tijd), addr, actie));
    }

    IEnumerator WachtEnVoerUit(float tijd, byte addr, System.Action<byte> actie)
    {
        if (tijd > 0f) yield return new WaitForSeconds(tijd);
        actie(addr);
    }

    bool HeeftVerbinding()
    {
        if (verbinding != null) return true;
        Debug.LogError("[ShowControl] Geen SerialController gekoppeld. " +
                       "Zet er één in de scene of vul het veld 'Verbinding' in.", this);
        return false;
    }
}
