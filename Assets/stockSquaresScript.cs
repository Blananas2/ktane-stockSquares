using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class stockSquaresScript : MonoBehaviour
{

    public KMAudio Audio;
    public KMBombModule Module;

    public KMSelectable ModuleSelectable;
    public SpriteRenderer[] SmallSlots;
    public KMSelectable[] SmallSels;
    public SpriteRenderer BigSlot;
    public Sprite[] Sprites;
    public Sprite Silly;
    public TextMesh NumberSequence;
    public KMSelectable[] NumberedButtons;
    public KMSelectable CrossButton;
    public KMSelectable CheckButton;

    int row = -1;
    int[] answer = { -1, -1, -1, -1, -1 };
    string inp = "";

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        ModuleSelectable.OnFocus += delegate () { BigSlot.sprite = moduleSolved ? null : Silly; };
        ModuleSelectable.OnDefocus += delegate () { BigSlot.sprite = null; };

        foreach (KMSelectable SmallSel in SmallSels)
        {
            SmallSel.OnHighlight += delegate () { ShowOnBig(SmallSel); };
        }

        foreach (KMSelectable NumberedButton in NumberedButtons)
        {
            NumberedButton.OnInteract += delegate () { NumberPress(NumberedButton); return false; };
        }

        CrossButton.OnInteract += delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, CrossButton.transform);
            CrossButton.AddInteractionPunch(0.5f);
            ClearInp();
            return false;
        };
        CheckButton.OnInteract += delegate () { CheckInp(); return false; };
    }

    // Use this for initialization
    void Start()
    {
        row = Rnd.Range(0, 10);
        Debug.LogFormat("[Stock Squares #{0}] Using row {1}", moduleId, row);
        for (int img = 0; img < 5; img++)
        {
            int col = Rnd.Range(0, 10);
            SmallSlots[img].sprite = Sprites[row * 10 + col];
            answer[img] = col;
        }
        Debug.LogFormat("[Stock Squares #{0}] Images used: {1}", moduleId, answer.Join(","));
        Debug.LogFormat("[Stock Squares #{0}] Correct number sequence: {1}", moduleId, answer.Join(""));
    }

    void NumberPress(KMSelectable N)
    {
        N.AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, N.transform);
        if (moduleSolved) { return; }
        for (int V = 0; V < 10; V++)
        {
            if (NumberedButtons[V] == N)
            {
                N.AddInteractionPunch(0.5f);
                if (inp.Length < 5)
                {
                    inp += V.ToString();
                    NumberSequence.text = inp;
                }
            }
        }
    }

    void ShowOnBig(KMSelectable Z)
    {
        if (moduleSolved) { return; }
        for (int S = 0; S < 5; S++)
        {
            if (SmallSels[S] == Z)
            {
                BigSlot.sprite = Sprites[row * 10 + answer[S]];
            }
        }
    }

    void CheckInp()
    {
        CheckButton.AddInteractionPunch(0.5f);
        if (moduleSolved)
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, CheckButton.transform);
            return;
        }
        if (inp == answer.Join(""))
        {
            Module.HandlePass();
            moduleSolved = true;
            Debug.LogFormat("[Stock Squares #{0}] Input is correct, module solved.", moduleId);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, Module.transform);
        }
        else
        {
            Module.HandleStrike();
            Debug.LogFormat("[Stock Squares #{0}] Input ({1}) is correct, strike!", moduleId, inp == "" ? "None" : inp);
        }
        ClearInp();
    }

    void ClearInp()
    {
        inp = "";
        NumberSequence.text = null;
    }

    // Twitch Plays by Kilo Bites

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} cycle [hovers through all of the squares one by one] || submit 12345 [submits the number you input]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        yield return null;

        if ("CYCLE".ContainsIgnoreCase(split[0]))
        {
            if (split.Length > 1)
            {
                yield return "sendtochaterror You added too many parameters!";
                yield break;
            }

            foreach (KMSelectable smallSquare in SmallSels)
            {
                smallSquare.OnHighlight();
                yield return new WaitForSeconds(1.5f);
            }

            yield break;
        }

        if ("SUBMIT".ContainsIgnoreCase(split[0]))
        {
            if (split.Length == 1)
            {
                yield return "sendtochaterror Please specify what numbers to submit!";
                yield break;
            }

            if (split.Length > 2)
            {
                yield return "sendtochaterror You added too many parameters!";
                yield break;
            }

            if (split[1].Length != 5)
            {
                yield return "sendtochaterror Please make sure that the number you have to submit is exactly 5 digits!";
                yield break;
            }

            if (!split[1].All(char.IsDigit))
            {
                yield return string.Format("sendtochaterror {0} is/aren't valid digit(s)!", split[1].Where(x => !char.IsDigit(x)).Join(", "));
                yield break;
            }

            var obtainDigits = split[1].Select(x => x - '0').ToArray();

            foreach (var digit in obtainDigits)
            {
                NumberedButtons[digit].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            CheckButton.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (!answer.Join("").StartsWith(inp))
        {
            CrossButton.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        for (int i = inp.Length; i < 5; i++)
        {
            NumberedButtons[answer[i]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        CheckButton.OnInteract();
        yield return new WaitForSeconds(0.1f);
    }
}
