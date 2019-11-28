using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EggCollector.Utils;
using EggCollector.Interfaces;
using Random = UnityEngine.Random;

[RequireComponent(typeof(SpriteRenderer))]
public class EggController : MonoBehaviour, IPoolableObject
{
    #region Fields
    private int score = 0;
    public int Score
    {
        get { return score * multiplyer; }
    }

    private int multiplyer = 1;
    public int Multiplyer
    {
        get { return multiplyer; }
    }

    public HenColors EggColor;

    [Header("Placeholder Sprites")]
    public Sprite YolkSprite;
    public Sprite EggSprite;

    [Header("Times")]
    public float StepTime = 0.5f;
    public float DestroyTime = 0.1f;
    public float SpawnTime = 0.1f;
    public float ScoreDelayTime = 1.25f;
    public float BasketAnimationDelay = 0.42f;

    [Header("Sounds")]
    public AudioClip SuccessClip;
    public AudioClip FailClip;
    public AudioClip[] PointsClips;
    public AudioClip[] MultiplierClips;

    [Header("Colors")]
    public Color[] PointsColor;
    public Color MultiplierColor;
    public Color FailColor;

    [Header("Font sizes")]
    public float ScoreBaseFontSize = 32f;
    public float MultiplierFontSize = 72f;
    public float ScoreLossFontSize = 52f;

    [Header("Points")]
    public int RowBonus = 200;
    public int ColumnBonus = 200;

    private SpriteRenderer SpRenderer;
    private IEnumerable<PathStep> Path;

    private int currentStep = 0;
    #endregion Fields

    void Start()
    {
        SpRenderer = GetComponent<SpriteRenderer>();
    }

    public void StartPath(IEnumerable<PathStep> path, HenColors color)
    {
        currentStep = 0;
        EggColor = color;

        //Reset score and multiplyer
        score = 0;
        multiplyer = 1;
        if (SpRenderer == null)
        {
            SpRenderer = GetComponent<SpriteRenderer>();
        }
        SpRenderer.enabled = false;
        SpRenderer.sprite = EggSprite;

        PlayManager.Instance.SetEggRolling(true);
        PlayManager.Instance.SubtractEgg();
        Path = path;
        var coroutine = MovePath(Path);
        StartCoroutine(coroutine);
    }

    private void Success()
    {
        PlayManager.Instance.AddToScore(Score);
        PlayManager.Instance.EggCompletedRoll();
    }

    private void Fail()
    {
        multiplyer = 0;
        SpRenderer.sprite = YolkSprite;
        PlayManager.Instance.EggCompletedRoll();
    }

    // every 2 seconds perform the print()
    private IEnumerator MovePath(IEnumerable<PathStep> path)
    {
        yield return new WaitForSeconds(StepTime);


        List<PipeBaseController> vistedPipes = new List<PipeBaseController>();
        //TODO int[] Rows & int[] Columns never used
        int[] Rows = new int[Grid.Instance.GridHeight];
        int[] Columns = new int[Grid.Instance.GridWidth];
        foreach (var pathstep in path)
        {
            transform.position = pathstep.Pipe.gameObject.transform.position;
            pathstep.Pipe.Enter(pathstep.EnterDirection);


            score += pathstep.Pipe.Score;
            if (vistedPipes.Contains(pathstep.Pipe))
            {
                multiplyer++;
                ShowMultiplier();
                PlayMultiplier();
            }
            else
            {
                vistedPipes.Add(pathstep.Pipe);
            }

            ShowScore();
            PlayAudioPoint();
            currentStep++;

            yield return new WaitForSeconds(StepTime);
        }

        if (path.Any())
        {
            PathStep lastStep = path.Last();
            if (lastStep.GridIndex.y == 0 && lastStep.ExitDirection == Direction.Down 
                && Grid.Instance.Baskets[lastStep.GridIndex.x] != null 
                && Grid.Instance.Baskets[lastStep.GridIndex.x].HenColor == EggColor)
            {
                transform.position = Grid.Instance.Baskets[lastStep.GridIndex.x].transform.position;

                Grid.Instance.Baskets[lastStep.GridIndex.x].AnimateFallingEgg(EggColor);
                yield return new WaitForSeconds(BasketAnimationDelay);
                ShowSuccess();
                PlaySuccess();
                Success();
                yield return new WaitForSeconds(StepTime);
            }
            else
            {
                ShowScoreLoss();
                PlayFail();
                Fail();
                yield return new WaitForSeconds(StepTime);
            }
        }
        else
        {
            Fail();
        }

        SpRenderer.enabled = false;

        Grid.Instance.ResetPathHighLight();

        //Delete used pipes on used indices:
        var deletedIndices = new List<IntVector2>();
        foreach (var pathstep in path)
        {
            //Delete if not already deleted
            if (!deletedIndices.Contains(pathstep.GridIndex) && pathstep.Pipe.DestoryOnEggRoll())
            {
                deletedIndices.Add(pathstep.GridIndex);
                Grid.Instance.DeletePipeOnIndex(pathstep.GridIndex);
                yield return new WaitForSeconds(DestroyTime);
            }
        }

        //Spawn new pipes on used indices
        foreach (var deletedIndex in deletedIndices)
        {
            var pipedir = LevelManager.Instance.GetNextPipe();
            if (pipedir != PipeDirectionTypes.None)
            {
                Grid.Instance.SpawnPipeOnIndex(deletedIndex, pipedir);
                yield return new WaitForSeconds(SpawnTime);
            }
        }

        Grid.Instance.SetPathHighLight();

        PlayManager.Instance.SetEggRolling(false);
        ReturnToPool();
    }

    private AudioClip GetAudioPoint()
    {
        return PointsClips[Mathf.Clamp(currentStep, 0, PointsClips.Length - 1)];
    }

    private void PlayAudioPoint()
    {
        GlobalSoundController.Instance.PlaySound(GetAudioPoint());
    }

    private Color GetScoreColor()
    {
        return PointsColor[Mathf.Clamp(currentStep, 0, PointsColor.Length - 1)];
    }

    private void ShowScore()
    {
        UIScoreSpawnerController.Instance.SpawnScore("" + Score, transform.position,
            Quaternion.identity, GetScoreColor(), ScoreBaseFontSize + currentStep * 2);
    }

    private AudioClip GetMultiplierAudio()
    {
        return MultiplierClips[Mathf.Clamp(multiplyer - 1, 0, MultiplierClips.Length - 1)];
    }

    private void PlayMultiplier()
    {
        GlobalSoundController.Instance.PlaySound(GetMultiplierAudio());
    }

    private void ShowMultiplier()
    {
        UIScoreSpawnerController.Instance.SpawnScore("X" + multiplyer + "!",
            transform.position, Quaternion.identity, MultiplierColor,
            MultiplierFontSize, UIScoreController.ScoreTextFadeType.Multiplyer);
    }

    private void ShowScoreLoss()
    {
        UIScoreSpawnerController.Instance.SpawnScore("" + Score, transform.position,
            Quaternion.identity, FailColor, ScoreLossFontSize,
            UIScoreController.ScoreTextFadeType.Loss);
    }

    private void ShowRowBonus(int row)
    {
        UIScoreSpawnerController.Instance.SpawnScore("+" + RowBonus + "!",
            Grid.Instance.GetRowWorldPosition(row), Quaternion.identity,
            MultiplierColor, ScoreBaseFontSize, UIScoreController.ScoreTextFadeType.Multiplyer);
    }

    private void ShowColumnBonus(int column)
    {
        UIScoreSpawnerController.Instance.SpawnScore("+" + ColumnBonus + "!",
            Grid.Instance.GetColumnWorldPosition(column), Quaternion.identity,
            MultiplierColor, ScoreBaseFontSize, UIScoreController.ScoreTextFadeType.Multiplyer);
    }


    private void PlayFail()
    {
        GlobalSoundController.Instance.PlaySound(FailClip);
    }

    private void PlaySuccess()
    {
        GlobalSoundController.Instance.PlaySound(SuccessClip);
    }

    private void ShowSuccess()
    {
        UIScoreSpawnerController.Instance.SpawnScore("" + Score, transform.position,
            Quaternion.identity, GetScoreColor(), ScoreBaseFontSize + currentStep * 2,
            UIScoreController.ScoreTextFadeType.Success);
    }


    /* >> IPoolableObject << */
    #region IPoolableObject
    protected ObjectPooler objectPooler;
    public virtual void ActivatedFromPool(Vector3 postion, Quaternion rotation, ObjectPooler pool)
    {
        objectPooler = pool;
    }

    public virtual void ReturnToPool()
    {
        objectPooler.ReturnToPool(this.gameObject);
    }

    public virtual void DeactivateToPool()
    {
        return;
    }
    #endregion

}
