using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class LevelProgressor : MonoBehaviour
{
    [Serializable]
    public struct LevelPrefsKeys
    {
        public string CurrentLevel;
        public string Score;
        public string Unlocked;
    }

    public static LevelProgressor Instance;

    [Header("Keys")]
    public LevelPrefsKeys Keys;
    [Header("Level")]
    public LevelCollection LevelCollection;

    private int currentLevel = -1;
    public int CurrentLevel
    {
        get
        {
            return currentLevel;
        }
    }

    void Start()
    {
        Instance = this;
        if (PlayerPrefs.HasKey(Keys.CurrentLevel))
        {
            currentLevel = PlayerPrefs.GetInt(Keys.CurrentLevel);
        }
        else
        {
            currentLevel = 0;
        }
    }

    private string GetScoreKey(string levelName)
    {
        return string.Format("{0}-{1}", Keys.Score, levelName);
    }

    private string GetUnlockedKey(string levelName)
    {
        return string.Format("{0}-{1}", Keys.Unlocked, levelName);
    }


    public void SetLevelComplete(Level level, int score, bool unlockNext)
    {
        SetLevelComplete(level.LevelName, score, unlockNext);
    }

    public void SetLevelComplete(string levelName, int score, bool unlockNext)
    {
        SetScore(levelName, score);
        //TODO var level never used
        var level = LevelCollection.Levels.FirstOrDefault(x => x.LevelName == levelName);

        if (unlockNext)
        {
            if (NextLevelPossible())
            {
                var nextlevel = LevelCollection.Levels[currentLevel + 1];
                SetUnlocked(nextlevel, true);
            }

        }

    }

    public void SetScore(Level level, int score)
    {
        SetScore(level.name, score);
    }

    public void SetScore(string levelName, int score)
    {
        var key = GetScoreKey(levelName);
        PlayerPrefs.SetInt(key, score);
    }


    public int GetScore(string levelName)
    {

        var key = GetScoreKey(levelName);
        if (PlayerPrefs.HasKey(key))
        {
            return PlayerPrefs.GetInt(key);
        }

        return 0;
    }

    public bool GetCompletedLevel(Level level)
    {
        return GetCompletedLevel(level.LevelName);
    }

    public bool GetCompletedLevel(string levelName)
    {
        var level = LevelCollection.Levels.FirstOrDefault(x => x.LevelName == levelName);
        if (level == null)
        {
            return false;
        }

        var score = GetScore(levelName);
        return score > level.Goals.Bronze;
    }

    public int GetNumberOfEarnedStars(Level level)
    {
        return GetNumberOfEarnedStars(level.LevelName);
    }

    public int GetNumberOfEarnedStars(string levelName)
    {
        var level = LevelCollection.Levels.FirstOrDefault(x => x.LevelName == levelName);
        var score = GetScore(levelName);

        var numberOfStar = 0;
        if (score >= level.Goals.Bronze)
        {
            numberOfStar++;
        }
        if (score >= level.Goals.Silver)
        {
            numberOfStar++;
        }
        if (score >= level.Goals.Gold)
        {
            numberOfStar++;
        }

        return numberOfStar;
    }


    public bool GetUnlocked(Level level)
    {
        return GetUnlocked(level.LevelName);
    }

    public bool GetUnlocked(string levelName)
    {
        var key = GetUnlockedKey(levelName);
        if (PlayerPrefs.HasKey(key))
        {
            return PlayerPrefs.GetInt(key) != 0;
        }

        return false;
    }

    public void SetUnlocked(Level level, bool unlocked)
    {
        SetUnlocked(level.LevelName, unlocked);
    }

    public void SetUnlocked(string levelName, bool unlocked)
    {
        var key = GetUnlockedKey(levelName);
        PlayerPrefs.SetInt(key, (unlocked ? 1 : 0));
        PlayerPrefs.Save();
    }

    public Level GetCurrentLevel()
    {
        if (currentLevel < 0 || currentLevel >= LevelCollection.Levels.Length)
        {
            return null;
        }
        var level = LevelCollection.Levels[currentLevel];
        SetUnlocked(level, true);

        return level;
    }

    public bool NextLevelPossible()
    {
        return (currentLevel + 1) < LevelCollection.Levels.Length;
    }

    public Level AdvanceToNextLevel()
    {
        //TODO: remove loop of levels;
        if (!NextLevelPossible())
        {
            currentLevel = 0;
        }
        else
        {
            currentLevel++;
        }
        PlayerPrefs.SetInt(Keys.CurrentLevel, currentLevel);
        PlayerPrefs.Save();
        return GetCurrentLevel();
    }

    public void DeleteAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}

