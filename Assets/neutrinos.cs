using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class neutrinos : MonoBehaviour
{
    public KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;
    public KMSelectable[] buttons;
    public TextMesh[] inputflavorlabels;
    public TextMesh[] inputbarlabels;
    public TextMesh[] outputflavorlabels;
    public TextMesh[] outputbarlabels;
    public TextMesh planetName;

    private string[] planetNames = { "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune" };
    private int[] flavorLabels = new int[6];
    private int[] barLabels = new int[6];
    private int[] startingFlavors = new int[3];
    private int[] finalFlavors = new int[3];
    private int planetIndex;
    private int o1index = 6;
    private int o2index = 6;
    private int o3index = 6;
    private int[] planetDists = { 387, 723, 1000, 1524, 5203, 9582, 19201, 30047 };
    private double neutrinoMass = 1.2;
    private int[] times = new int[3];
    private List<int> fastesttimes;
    private bool brokenphysics = false;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    private bool moduleReady = false;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
        {
            KMSelectable pressedButton = button;
            button.OnInteract += delegate () { ButtonPress(pressedButton); return false; };
        }
    }



    // Use this for initialization
    void Start()
    {
        GenerateFlavors();
        for(int i = 0; i < 3; i++)
        {
            string bar = (startingFlavors[i] > 2) ? " bar" : "";
            Debug.LogFormat("[Neutrinos #{0}] Neutrino " + (i+1).ToString() + " starting flavor is " + GetFlavorLabel(startingFlavors[i]) + bar,moduleId);
            finalFlavors[i] = FindNeutrino(startingFlavors[i], i + 1);
            bar = (finalFlavors[i] > 2) ? " bar" : "";
            if (finalFlavors[i] == 6) bar = "nope";
            Debug.LogFormat("[Neutrinos #{0}] Neutrino " + (i + 1).ToString() + " final flavor is " + GetFlavorLabel(finalFlavors[i]) + bar, moduleId);
        }
        fastesttimes = times.ToList();
        fastesttimes.Sort();
        FindAnnihilations();      
        moduleReady = true;
    }

    void FindAnnihilations()
    {
        for (int i = 0; i < 3; i++)
        {
            int i1 = times.ToList().IndexOf(fastesttimes[i]);
            int f = finalFlavors[i1];
            if (f != 6)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (j != i)
                    {
                        int j1 = times.ToList().LastIndexOf(fastesttimes[j]);
                        if ((finalFlavors[j1] == f + 3 || finalFlavors[j1] == f - 3) && finalFlavors[j1] != 6)                 
                        {
                            finalFlavors[i1] = 6;
                            finalFlavors[j1] = 6;
                            Debug.LogFormat("[Neutrinos #{0}] Neutrinos " + (Math.Min(i1, j1) + 1).ToString() + " and " + (Math.Max(i1, j1) + 1).ToString() + " have annihilated! Submit these as empty neutrinos instead.", moduleId);
                            return;
                        }
                    }
                }
            }
        }
    }

    void GenerateFlavors()
    {       
        for (int i = 0; i < 3; i++)
        {
            flavorLabels[i] = UnityEngine.Random.Range(0, 3);
            barLabels[i] = UnityEngine.Random.Range(0, 2);
            inputflavorlabels[i].text = GetFlavorLabel(flavorLabels[i]);
            inputbarlabels[i].text = GetBarLabel(barLabels[i]);
            startingFlavors[i] = flavorLabels[i];
            if (barLabels[i] == 1) startingFlavors[i] += 3;
        }
        planetIndex = UnityEngine.Random.Range(0, 8);
        planetName.text = planetNames[planetIndex];
    }

    string GetFlavorLabel(int i)
    {
        switch (i)
        {
            case 0:
            case 3:
                return "e";
            case 1:
            case 4:
                return "μ";
            case 2:
            case 5:
                return "τ";
            default: return "";
        }
    }
    
    string GetBarLabel(int i)
    {
        return i == 0 ? "" : "-";
    }

    private int pressindex = 0;
    private bool b1pressed = false;
    private bool b2pressed = false;
    private bool b3pressed = false;
    private bool stopcounting = false;

    void ButtonPress(KMSelectable button)
    {
        if (moduleReady)
        {
            button.AddInteractionPunch();
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);
            if (pressindex > 2)
            {
                pressindex = 2;
                stopcounting = true;
            }
            CheckAnnihilationCase();
            switch (button.name)
            {                
                case "Screen":
                    if ((o1index != finalFlavors[0] || o2index != finalFlavors[1] || o3index != finalFlavors[2]) && !brokenphysics)
                    {
                        module.HandleStrike();
                        Debug.LogFormat("[Neutrinos #{0}] Incorrect neutrinos submitted! Strike!", moduleId);
                    }
                    else
                    {
                        module.HandlePass();
                        moduleReady = false;
                        Debug.LogFormat("[Neutrinos #{0}] Module Solved", moduleId);
                    }
                    break;
                case "Output1":
                    if (fastesttimes[pressindex] == times[0] || b1pressed)
                    {
                        o1index = NextLabel(o1index, outputflavorlabels[0], outputbarlabels[0]);
                        if (!b1pressed)
                        {
                            if(!stopcounting) pressindex++;
                            b1pressed = true;
                        }
                    }
                    else
                    {
                        module.HandleStrike();
                        Debug.LogFormat("[Neutrinos #{0}] Other neutrinos arrived sooner! Strike!", moduleId);
                    }
                    break;
                case "Output2":
                    if (fastesttimes[pressindex] == times[1] || b2pressed)
                    {
                        o2index = NextLabel(o2index, outputflavorlabels[1], outputbarlabels[1]);
                        if (!b2pressed)
                        {
                            if (!stopcounting) pressindex++;
                            b2pressed = true;
                        }
                    }
                    else
                    {
                        module.HandleStrike();
                        Debug.LogFormat("[Neutrinos #{0}] Other neutrinos arrived sooner! Strike!", moduleId);
                    }
                    break;
                case "Output3":
                    if (fastesttimes[pressindex] == times[2] || b3pressed)
                    {
                        o3index = NextLabel(o3index, outputflavorlabels[2], outputbarlabels[2]);
                        if (!b3pressed)
                        {
                            if (!stopcounting) pressindex++;
                            b3pressed = true;
                        }
                    }
                    else
                    {
                        module.HandleStrike();
                        Debug.LogFormat("[Neutrinos #{0}] Other neutrinos arrived sooner! Strike!", moduleId);
                    }
                    break;
            }
        }
    }

    void CheckAnnihilationCase()
    {
        if (finalFlavors.Contains(6))
        {
            b1pressed = b2pressed = b3pressed = true;
        }
    }

    int NextLabel(int index, TextMesh flavor, TextMesh bar)
    {
        index++;
        index = index % 7;
        flavor.text = GetFlavorLabel(index);
        if (index < 3 || index == 6) bar.text = "";
        else if (index < 6) bar.text = "-";

        return index;
    }

    int FindNeutrino(int flavor, int pos)
    {
        double mom = 0;
        switch (pos)
        {
            case 1:
                int bathols = bomb.GetBatteryHolderCount() % 10;
                int ports = bomb.GetPortCount() % 10;
                int holandports = bathols * 100 + ports * 10 + pos ;
                holandports %= 301;
                mom = 1.2 - (double)holandports / 1000;
                break;
            case 2:
                int portplates = bomb.GetPortPlateCount() % 10;
                int bats = bomb.GetBatteryCount() % 10;
                int batandplate = portplates * 100 + bats * 10 + pos;
                batandplate %= 302;
                mom = 1.2 - (double)batandplate / 1000;
                break;
            case 3:
                int unlitinds = bomb.GetOffIndicators().Count() % 10;
                int litinds = bomb.GetOnIndicators().Count() % 10;
                int inds = unlitinds * 100 + litinds * 10 + pos;
                inds %= 333;
                mom = 1.2 - (double)inds / 1000;
                break;
        }
        Debug.LogFormat("[Neutrinos #{0}] Neutrino " + pos + " momentum is " + mom, moduleId);
        double beta = Math.Round(mom / neutrinoMass,3,MidpointRounding.AwayFromZero);
        Debug.LogFormat("[Neutrinos #{0}] Neutrino " + pos + " speed is " + beta, moduleId);
        if (mom == 1.2)
        {
            Debug.LogFormat("[Neutrinos #{0}] You broke physics... Just submit because nothing is real anymore.", moduleId);
            brokenphysics = true;
            return 6;
        }
        double gamma = Math.Round(Gamma(beta),3,MidpointRounding.AwayFromZero);
        Debug.LogFormat("[Neutrinos #{0}] Neutrino " + pos + " gamma is " + gamma, moduleId, moduleId);
        double length = Math.Round(planetDists[planetIndex] / gamma,3,MidpointRounding.AwayFromZero);
        Debug.LogFormat("[Neutrinos #{0}] Neutrino " + pos + " distrance travelled is " + length, moduleId);
        int time = (int) Math.Floor(length / beta);
        Debug.LogFormat("[Neutrinos #{0}] Neutrino " + pos + " travel time is " + time, moduleId);
        times[pos - 1] = time;
        time = time % 10 + 1;
        return PerformCycle(flavor, time);
    }

    int PerformCycle(int flavor, int t)
    {
        int start = 0;
        bool anti = flavor > 2;
        int lap = 1;
        switch (flavor)
        {
            case 0:
            case 3:
                start = 1;
                break;
            case 1:
            case 4:
                start = 7;
                break;
            case 2:
            case 5:
                start = 4;
                break;     
        }
        int pos = start;
        while(t > 0)
        {           
            switch (pos)
            {
                case 0: if (lap == 1) t--; break;
                case 1: if (anti) t--; break;
                case 2: if (bomb.GetSerialNumberLetters().ToList().Contains('E')) t--; break;
                case 3: if (bomb.GetSerialNumberLetters().ToList().Contains('T')) t--; break;
                case 4: if (planetIndex < 4) t--; break;
                case 5: if (bomb.GetPortCount() > 2) t--; break;
                case 6: if (bomb.GetBatteryCount() > 2) t--; break;
                case 7: if (bomb.GetSerialNumberLetters().ToList().Contains('M')) t--; break;
                case 8: if (lap > 1) t--; break;
            }
            if (t > 0) pos = anti ? pos - 1 : pos + 1;
            if (pos > 8) pos = 0;
            if (pos < 0) pos = 8;
            if (pos == start) lap++;
        }
        if (pos < 3) flavor = 0;
        else if (pos < 6) flavor = 2;
        else flavor = 1;
        if (anti) flavor += 3;
        return flavor;
    }

    double Gamma(double beta)
    {
        return 1 / (Math.Sqrt(1 - beta * beta));
    }

    string ListToString(List<string> l)
    {
        string str = "";
        foreach (var s in l) str += s;
        return str;
    }

    string ListToString(List<int> l)
    {
        string str = "";
        foreach (var s in l) str += s.ToString();
        return str;
    }

    string ListToString(List<char> l)
    {
        string str = "";
        foreach (var s in l) str += s.ToString();
        return str;
    }

    //twitch plays
    private bool inputIsValid(string cmd)
    {
        string[] validstuff = { "1", "2", "3" ,"submit"};
        if (validstuff.Contains(cmd.ToLower()))
        {
            return true;
        }
        return false;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <1/2/3/submit> [Presses the specified button]. You can also string presses together i.e. press 1 1 2 3, press 1,1,2,3";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ', ',');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            for (int i = 1; i < parameters.Length; i++)
            {
                if (inputIsValid(parameters[i]))
                {
                    yield return null;
                    if (parameters[i].ToLower().Equals("1"))
                    {
                        buttons[0].OnInteract();
                    }
                    else if (parameters[i].ToLower().Equals("2"))
                    {
                        buttons[1].OnInteract();
                    }
                    else if (parameters[i].ToLower().Equals("3"))
                    {
                        buttons[2].OnInteract();
                    }
                    else if (parameters[i].ToLower().Equals("submit"))
                    {
                        buttons[3].OnInteract();
                    }
                }
            }
            yield break;
        }
    }
}
 