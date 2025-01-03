﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using Mode = Helper.Mode;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;

public class GameController : MonoBehaviour
{
    public float fallTime = 0.8f;
    private float N = 15;
    public Vector3 startPos = new Vector3();
    private readonly Vector3[] Pivots = new[] { new Vector3(-0.33f, 0f, -1f), new Vector3(-0.27f, -0.15f, -1f), new Vector3(-0.27f, 0.1f, -1f), new Vector3(-0.12f, -0.1f, -1f), new Vector3(-0.22f, -0.1f, -1f), new Vector3(-0.02f, -0.1f, -1f), new Vector3(-0.2f, 0.1f, -1f) };

    private float previousTime, previousToLeft, previousToRight;
    private int score = 0;
    private int linesDeleted = 0;
    private int numGems = 0;
    private float playTime;
    private int nextLevel;
    private bool isCounting = false;
    private List<int> deletingRow = new List<int>();

    private int currStage = 0;

    private HashSet<int> deck = new HashSet<int>();

    private Block[,] grid = new Block[Helper.HEIGHT, Helper.WIDTH];

    public TetrisBlock[] Blocks;

    public GhostBlock[] Ghosts;
    private int nextBlock, scoreStage, scoreInf;
    public TetrisBlock nextBlockObject;
    public TetrisBlock currBlock;
    public TetrisBlock deadBlock;
    public GameObject nextBlockBackground, infoText, restartButton, restartIcon, resumeButton, pauseButton, speakerButton, muteButton, homeButton;
    public GameObject yesButton, noButton, replayFrame;
    public GemBlock gemBlock;
    private GhostBlock ghostBlock;
    private bool hardDropped, gameOver, gameClear, isDestroying, isPaused, isShowingAnimation, isRowDown, isAnimating, isEndTurn, isRestart;
    private ModeController controller;
    public Text timeValue, levelValue, linesValue, stageValue, scoreValue, gameModeValue, scoreHis, hisScoreText;

    void Start()
    {
        muteButton.SetActive(false);
        speakerButton.SetActive(true);
        restartButton.SetActive(true);
        InitGame();
        score = 0;
    }

    void InitGame()
    {
        scoreStage = PlayerPrefs.GetInt("ScoreStage", 0);
        scoreInf = PlayerPrefs.GetInt("ScoreInf", 0);
        FindObjectOfType<AudioManager>().Play("GameStart");
        controller = GameObject.FindWithTag("ModeController").GetComponent<ModeController>();
        gameModeValue.text = "M O D E :  " + (controller.GetMode() == Mode.stage ? "M À N  C H Ơ I" : "V Ô  H Ạ N");
        infoText.SetActive(false);
        yesButton.SetActive(false);
        noButton.SetActive(false);
        replayFrame.SetActive(false);
        resumeButton.SetActive(false);
        restartIcon.SetActive(true);
        restartButton.SetActive(true);
        homeButton.SetActive(false);
        gameOver = false;
        gameClear = false;
        isShowingAnimation = false;
        isEndTurn = false;
        isAnimating = false;
        playTime = 0;
        levelValue.text = "0";

        if (controller.GetMode() == Mode.stage)
        {
            SetStage();
            hisScoreText.text = "M À N  C A O  N H Ấ T";
        }
        else SetInf();
        NextBlock();
        NewBlock();

    }

    public void Pause()
    {
        if (isCounting)
        {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }
        isPaused = true;
        pauseButton.SetActive(false);
        resumeButton.SetActive(true);
        FindObjectOfType<AudioManager>().Mute("GameStart", true);
        muteButton.SetActive(true);
        speakerButton.SetActive(false);
    }

    public void Restart()
    {
        if(isCounting)
        {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }
        EventSystem.current.SetSelectedGameObject(null);
        currStage = 0;
        numGems = 0;
        score = 0;
        linesDeleted = 0;
        nextLevel = 0;
        fallTime = 0.8f;
        Resume();
        InitGame();
    }

    public void Resume()
    {
        if (isCounting)
        {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }
        isPaused = false;
        resumeButton.SetActive(false);
        pauseButton.SetActive(true);
        FindObjectOfType<AudioManager>().Mute("GameStart", false);
        muteButton.SetActive(false);
        speakerButton.SetActive(true);
    }

    public void Mute(bool isMute)
    {
        FindObjectOfType<AudioManager>().Mute("GameStart", isMute);
        if (!isMute)
        {
            muteButton.SetActive(false);
            speakerButton.SetActive(true);
        }
        else
        {
            muteButton.SetActive(true);
            speakerButton.SetActive(false);
        }
    }

    void NextBlock()
    {
        print("nextblock start");
        if (deck.Count == Blocks.Length) deck.Clear();
        do nextBlock = Random.Range(0, Blocks.Length);
        while (deck.Contains(nextBlock));
        deck.Add(nextBlock);

        if (nextBlockObject != null) nextBlockObject.Destroy();
        nextBlockObject = Instantiate(Blocks[nextBlock]);
        nextBlockObject.transform.parent = nextBlockBackground.transform;
        nextBlockObject.transform.localPosition = Pivots[nextBlock];
        print("nextblock end");
    }

    void SetStage()
    {
        if (currBlock != null) currBlock.Destroy();
        for (int y = 0; y < Helper.HEIGHT; y++)
        {
            for (int x = 0; x < Helper.WIDTH; x++)
            {
                if (grid[y, x] != null) grid[y, x].Destroy();
                int blockType = Helper.Stages[currStage, Helper.HEIGHT - y - 1, x];
                switch (blockType)
                {
                    case 0:
                        grid[y, x] = null;
                        break;
                    case 1:
                        grid[y, x] = Instantiate(deadBlock, new Vector3(x, y, 0), Quaternion.identity);
                        break;
                    case 2:
                        numGems++;
                        grid[y, x] = Instantiate(gemBlock, new Vector3(x, y, 0), Quaternion.identity);
                        break;
                }
            }
        }
    }

    void SetInf()
    {
        if (currBlock != null) currBlock.Destroy();
        for (int y = 0; y < Helper.HEIGHT; y++)
        {
            for (int x = 0; x < Helper.WIDTH; x++)
            {
                if (grid[y, x] != null)
                {
                    grid[y, x].Destroy();
                    grid[y, x] = null;
                }

            }
        }
    }

    void Update()
    {
        if (isPaused && Input.GetKeyDown(KeyCode.P)) Resume();

        else if (!isEndTurn && !gameOver && !gameClear && !isPaused && !isShowingAnimation)
        {
            if (Input.GetKey(KeyCode.LeftArrow) && Time.time - previousToLeft > 0.1f)
            {
                HorizontalMove(Vector3.left);
                previousToLeft = Time.time;
            }
            else if (Input.GetKey(KeyCode.RightArrow) && Time.time - previousToRight > 0.1f)
            {
                HorizontalMove(Vector3.right);
                previousToRight = Time.time;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Rotate();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                while (ValidMove(currBlock.transform) && !hardDropped) VerticalMove(Vector3.down);
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                hardDropped = false;
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                Pause();
            }
            if (Input.GetKeyDown(KeyCode.Escape)) // Nhấn Space để test
            {
                GoBack();
            }
            if (Time.time - previousTime > (Input.GetKey(KeyCode.DownArrow) ? fallTime / 10 : fallTime))
            {
                VerticalMove(Vector3.down);
                previousTime = Time.time;
            }
            if (isAnimating && !isEndTurn)
            {
                EndTurn();
                isEndTurn = false;
            }
            
            if (grid[18, 4] != null || gameOver) GameFinish("T H U A  R Ồ I");
            
            if (controller.GetMode() == Mode.stage && numGems == 0)
            {
                gameClear = true;
                GameClear();
            }
            
            nextLevel = Mathf.FloorToInt(linesDeleted / N);
            if (controller.GetMode() == Mode.stage) nextLevel = 0;
            
            playTime += Time.deltaTime;
            int minutes = Mathf.RoundToInt((playTime % (60 * 60 * 60)) / (60 * 60)), seconds = Mathf.RoundToInt((playTime % (60 * 60)) / 60), microseconds = Mathf.RoundToInt(playTime % 60);
            timeValue.text = String.Format("{0}:{1}:{2}", (minutes < 10 ? "0" : "") + minutes.ToString(), (seconds < 10 ? "0" : "") + seconds.ToString(), (microseconds < 10 ? "0" : "") + microseconds.ToString());

            GhostBlockImgUpdate();
            if(currStage < 10) InfoUpdate();
        }
        if (currStage == 10) GameFinish("HOÀN  THÀNH");
    }

    private void InfoUpdate()
    {
        linesValue.text = linesDeleted.ToString();

        if (controller.GetMode() == Mode.stage)
        {
            scoreValue.text = "-";
            stageValue.text = (currStage + 1).ToString();
            levelValue.text = "0";
            if ((currStage + 1) >= scoreStage)
            {
                scoreHis.text = (currStage + 1).ToString();

                // Lưu điểm số
                PlayerPrefs.SetInt("ScoreStage", (currStage + 1));
                PlayerPrefs.Save();
            }
            else scoreHis.text = scoreStage.ToString();
        }
        else
        {
            scoreValue.text = score.ToString();
            stageValue.text = "-";

            if (Int16.Parse(levelValue.text) < nextLevel && nextLevel < 5)
            {
                levelValue.text = nextLevel.ToString();
                fallTime -= 0.15f;
            }
            if (score >= scoreInf)
            {
                scoreHis.text = score.ToString();

                // Lưu điểm số
                PlayerPrefs.SetInt("ScoreInf", score);
                PlayerPrefs.Save();

            }
            else scoreHis.text = scoreInf.ToString();
        }

    }

    private void GhostBlockImgUpdate()
    {
        if (!ghostBlock.IsDestroyed())
        {
            ghostBlock.transform.position = GhostPosition(currBlock.transform.position);
        }
    }

    void Rotate()
    {
        Transform currTransform = currBlock.transform;
        currTransform.RotateAround(currTransform.TransformPoint(currBlock.rotationPoint), Vector3.forward, 90);
        ghostBlock.transform.RotateAround(ghostBlock.transform.TransformPoint(currBlock.rotationPoint), Vector3.forward, 90);

        if (!ValidMove(currBlock.transform))
        {
            currTransform.RotateAround(currTransform.TransformPoint(currBlock.rotationPoint), Vector3.forward, -90);
            ghostBlock.transform.RotateAround(ghostBlock.transform.TransformPoint(currBlock.rotationPoint), Vector3.forward, -90);

        }
    }

    void HorizontalMove(Vector3 nextMove)
    {
        currBlock.transform.position += nextMove;
        if (!ValidMove(currBlock.transform))
        {
            currBlock.transform.position -= nextMove;
        }
    }

    void VerticalMove(Vector3 nextMove)
    {
        currBlock.transform.position += nextMove;
        if (!ValidMove(currBlock.transform))
        {
            currBlock.transform.position -= nextMove;
            CreateDeadBlock();
            DestroyCurrBlock();
            CheckForLines();
        }
    }

    private void CreateDeadBlock()
    {
        foreach (Transform children in currBlock.transform)
        {
            int roundedY = Mathf.RoundToInt(children.transform.position.y);
            int roundedX = Mathf.RoundToInt(children.transform.position.x);
            Color currColor = children.GetComponent<SpriteRenderer>().color;
            if (grid[roundedY, roundedX] == null)
            {
                TetrisBlock curr = Instantiate(deadBlock, new Vector3(roundedX, roundedY, 0), Quaternion.identity);
                curr.sprite.GetComponent<SpriteRenderer>().color = currColor;
                grid[roundedY, roundedX] = curr;
            }
        }
    }

    private void DestroyCurrBlock()
    {
        currBlock.Destroy();
        ghostBlock.Destroy();
    }

    private void CheckForLines()
    {
        isShowingAnimation = true;
        deletingRow.Clear();

        for (int y = Helper.HEIGHT - 1; y >= 0; y--)
        {
            if (HasLine(y))
            {
                deletingRow.Add(y);
            }
        }
        linesDeleted += deletingRow.Count;
        score = 5 * linesDeleted;
        if (deletingRow != null)
        {
            isAnimating = true;
        }
    }

    private bool HasLine(int y)
    {
        for (int x = 0; x < Helper.WIDTH; x++)
        {
            if (grid[y, x] == null) return false;
        }
        return true;
    }

    private void EndTurn()
    {
        isEndTurn = true;
        print("EndTurn");
        FindObjectOfType<AudioManager>().Play("Blip");
        hardDropped = true;
        foreach (var y in deletingRow)
        {
            StartCoroutine(DeleteLine(y));
            StartCoroutine(WaitForRowDown(y));
        }
        StartCoroutine(WaitForNewBlock());
        isAnimating = false;
    }

    private IEnumerator DeleteLine(int y)
    {
        print("deleteline");
        isDestroying = true;
        int[] destroyedBlocks = new int[1];
        destroyedBlocks[0] = 0;
        for (int x = 0; x < Helper.WIDTH; x++)
        {
            if (grid[y, x] != null)
            {
                StartCoroutine(DeleteLineEffect(grid[y, x], destroyedBlocks));
            }
        }

        while (destroyedBlocks[0] < 10)
        {
            yield return new WaitForSeconds(0.1f);
        }
        for (int x = 0; x < Helper.WIDTH; x++)
        {
            if (grid[y, x] == null) continue;
            if (grid[y, x].transform.GetComponent<GemBlock>() != null) numGems--;
            grid[y, x].Destroy();
            grid[y, x] = null;
        }
        isDestroying = false;
        destroyedBlocks[0] = 0;
    }

    private IEnumerator DeleteLineEffect(Block dead, int[] destroyedBlocks)
    {
        Color tmp = dead.sprite.GetComponent<SpriteRenderer>().color;
        float _progress = 1f;

        while (_progress > 0.0f)
        {
            dead.sprite.GetComponent<SpriteRenderer>().color = new Color(tmp.r, tmp.g, tmp.b, tmp.a * _progress);
            _progress -= 0.1f;
            yield return new WaitForSeconds(0.03f);
        }

        if (_progress < 0.0f && dead != null)
        {
            destroyedBlocks[0]++;
        }
    }

    private IEnumerator WaitForRowDown(int y)
    {
        while (isDestroying)
        {
            yield return new WaitForSeconds(0.01f);
        }
        RowDown(y);
    }

    private IEnumerator WaitForNewBlock()
    {
        while (isDestroying || isRowDown)
        {
            yield return new WaitForSeconds(0.01f);
        }
        NewBlock();
    }

    void RowDown(int deletedLine)
    {
        print("rowdown");

        isRowDown = true;
        for (int y = deletedLine; y < Helper.HEIGHT; y++)
        {
            for (int x = 0; x < Helper.WIDTH; x++)
            {
                if (y == deletedLine)
                {
                    grid[y, x] = null;
                }
                if (grid[y, x] != null)
                {
                    grid[y - 1, x] = grid[y, x];
                    grid[y, x] = null;
                    grid[y - 1, x].transform.position -= Vector3.up;
                }
            }
        }
        isRowDown = false;
    }

    bool ValidMove(Transform transform)
    {
        foreach (Transform children in transform)
        {
            int roundedY = Mathf.RoundToInt(children.transform.position.y);
            int roundedX = Mathf.RoundToInt(children.transform.position.x);
            if (roundedX < 0 || roundedX >= Helper.WIDTH || roundedY < 0 || roundedY >= Helper.HEIGHT)
            {
                return false;
            }
            if (grid[roundedY, roundedX] != null)
            {
                return false;
            }
        }
        return true;
    }

    public Vector3 GhostPosition(Vector3 vec)
    {
        int x = Mathf.RoundToInt(vec.x), y = Math.Max(Mathf.RoundToInt(vec.y), 0), z = Mathf.RoundToInt(vec.z);
        ghostBlock.transform.position = new Vector3(x, y, z);
        while (ValidMove(ghostBlock.transform)) ghostBlock.transform.position += Vector3.down;

        return ghostBlock.transform.position + Vector3.up;
    }

    public void NewBlock()
    {
        print("newblock start");
        currBlock = Instantiate(Blocks[nextBlock], startPos, Quaternion.identity);
        isShowingAnimation = false;

        NewGhost();
        NextBlock();
        print("newblock end");
    }

    private void NewGhost()
    {
        print("newghost start");
        if (ghostBlock != null)
        {
            ghostBlock.Destroy();
            ghostBlock = Instantiate(Ghosts[nextBlock], currBlock.transform.position, Quaternion.identity);
        }
        else
        {
            ghostBlock = Instantiate(Ghosts[nextBlock], currBlock.transform.position, Quaternion.identity);
        }
        print("newghostend");
    }

    private void GameFinish(string finish)
    {
        if (ghostBlock != null) ghostBlock.Destroy();
        yesButton.SetActive(true);
        noButton.SetActive(true);
        replayFrame.SetActive(true);
        homeButton.SetActive(true);
        infoText.SetActive(true);
        gameOver = true;
        infoText.GetComponent<TextMeshProUGUI>().text = finish;
        
        FindObjectOfType<AudioManager>().Stop("GameStart");
    }

    private void GameClear()
    {
        currStage+=1;

        if (currStage < 10)
        { 
            if (ghostBlock != null) ghostBlock.Destroy();
            infoText.SetActive(true);
            infoText.GetComponent<TextMeshProUGUI>().text = "C H U Y Ể N  M À N";
            FindObjectOfType<AudioManager>().Stop("GameStart");
            FindObjectOfType<AudioManager>().Play("GameClear");
            StartCoroutine(CountDown());
        }
    }

    private IEnumerator CountDown()
    {
        isCounting = true;
        yield return new WaitForSeconds(0.5f);
        infoText.GetComponent<TextMeshProUGUI>().text = "3";
        yield return new WaitForSeconds(0.5f);
        infoText.GetComponent<TextMeshProUGUI>().text = "2";
        yield return new WaitForSeconds(0.5f);
        infoText.GetComponent<TextMeshProUGUI>().text = "1";
        yield return new WaitForSeconds(0.5f);
        isCounting = false;

        //InitGame();
        gameClear = false;
        infoText.SetActive(false);
        SetStage();
        NextBlock();
        NewBlock();
    }

    public void GoBack()
    {
        SceneManager.LoadScene("ModeScene");
    }
}
