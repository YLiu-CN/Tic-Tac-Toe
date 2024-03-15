using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    
    [Header("素材资源")]
    public GameObject board;
    public GameObject cross;
    public GameObject nought;
    public Sprite mapOnSprite;
    public AudioClip  clickSound;

    [Header("UI")]
    public Canvas canvas;
    public Text stateText;
    public Text orderText;
    public InputField difficultField;
    public List<Button> mapButtonList;


    private Camera cam;

    private int roundCount = 0;
    private List<square> squareList;
    private square lastPlayerPiece;
    private square lastComputerPiece;

    private bool isPlayerFirst = true;
    private bool canPlayerPut = false;
    private int winState = -1;
    private bool canAdvantageAtStart = false;
    private bool hasAdvantage = false;

    static int map = 3;
    static int difficult = 50;
    static int infinite = 99;


    private class square
    {
        public Transform Transform;
        public int2 ID;
        public int State;
        public int Weight;
    }


    private void Awake() 
    {
        cam = Camera.main;
        squareList = new List<square>();
        canAdvantageAtStart = ((map - 1) % 2) == 0;
    }

    void Start()
    {
        PrepareScene();
        DecideOrder();
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if(Physics.Raycast(ray,out hitInfo))
            {
                PlaySoundAtAction();
                if(canPlayerPut)
                {
                    square s = squareList.Find(s=>s.Transform == hitInfo.transform);
                    if(s.State == 0) 
                    {
                        PutPiece(s);

                        if(winState == -1) StartCoroutine(WaitComputer());
                        else ShowResult();
                    }
                }
            }               
        }
    }

    private void PrepareScene()
    {
        for(int a = 0; a < map; a++)
        {
            for(int b = 0; b < map; b++)
            {
                Vector3 pos = new Vector3(0.5f + b, 0.5f + a, 0f);
                GameObject board = Instantiate(this.board, pos, Quaternion.identity);

                int2 num = new int2(b, a);
                squareList.Add(new square(){
                    Transform = board.transform,
                    ID = num,
                    State = 0,
                    Weight = -1
                });
            }
        }

        float center = map / 2f;
        cam.transform.position = new Vector3(center * 2f, center, -10f);
        cam.orthographicSize = center * 1.3f;

        canvas.transform.position = new Vector3(center * 2f, center, 0f);
        mapButtonList[map - 3].GetComponentInChildren<Text>().color = cam.backgroundColor;
        mapButtonList[map - 3].GetComponent<Image>().sprite = mapOnSprite;
        difficultField.text = difficult.ToString();
    }

    void DecideOrder()
    {
        isPlayerFirst = UnityEngine.Random.value < 0.5f;
        if(isPlayerFirst)
        {
            canPlayerPut = true;
            orderText.text = "玩家\n先手";
        } 
        else
        {
            orderText.text = "电脑\n先手";
            StartCoroutine(WaitComputer());
        }
    }

    IEnumerator WaitComputer()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        square s = GetPutSquare();
        PutPiece(s);

        if(winState != -1) ShowResult();
    }

    private void ShowResult()
    {
        switch(winState)
        {
            case 0: 
                print("平局");
                stateText.text = "平 局";
                break;
            case 1: 
                print("胜利");
                stateText.text = "胜 利";
                break;
            case 2: 
                print("失败");
                stateText.text = "失 败";
                break;
        }
        stateText.fontSize = 260;
    }

    private void PutPiece(square square)
    {
        if(square is null) 
        {
            print("获取位置错误");
            return;
        }
        if(canPlayerPut)
        {
            Instantiate(cross, square.Transform.position, Quaternion.identity);
            square.State = 1;
            lastPlayerPiece = square; 
            if(!isPlayerFirst) roundCount++;

            if(CheckWin(square)) winState = 1;
            else if(CheckDraw(square)) winState = 0;

            canPlayerPut = false;
        }
        else
        {
            Instantiate(nought, square.Transform.position, Quaternion.identity);
            square.State = 2;
            PlaySoundAtAction();
            lastComputerPiece = square;
            if(isPlayerFirst) roundCount++;

            if(CheckWin(square)) winState = 2;
            else if(CheckDraw(square)) winState = 0;

            if(winState == -1) canPlayerPut = true;
        }
    }

    private square GetPutSquare()
    {
        if(roundCount == 0)
        {
            if(canAdvantageAtStart && UnityEngine.Random.Range(0, 100) < difficult)
            {             
                if(isPlayerFirst)
                {
                    int plyrX = lastPlayerPiece.ID.x;
                    int plyrY = lastPlayerPiece.ID.y;

                    if(plyrX == plyrY && plyrX == map - 1 - plyrY)
                    {
                        List<square> cornerList = GetCornerSquares();
                        if(cornerList.Count != 0) return GetRandomSquare(cornerList);
                    }
                    else if (plyrX == plyrY || plyrX == map - 1 - plyrY)
                    {
                        int mid = (map - 1) / 2;
                        int centerIndex = (map + 1) * mid;
                        return squareList[centerIndex];
                    }
                    else
                    {
                        int mid = (map - 1) / 2;
                        int centerIndex = (map + 1) * mid;
                        List<square> possibleList = new List<square>(){squareList[centerIndex]};
                        if(plyrX == 0)
                        {
                            int leftUpIndex = map * (map - 1);
                            int leftDownIndex = 0;
                            int oppositeEdgeIndex = map * (plyrY + 1) - 1;
                            possibleList.Add(squareList[leftUpIndex]);
                            possibleList.Add(squareList[leftDownIndex]);
                            possibleList.Add(squareList[oppositeEdgeIndex]);
                        }
                        else if(plyrX == map - 1)
                        {
                            int rightUpIndex = (map + 1)* (map - 1);
                            int rightDownIndex = map - 1;
                            int oppositeEdgeIndex = map * plyrY;
                            possibleList.Add(squareList[rightUpIndex]);
                            possibleList.Add(squareList[rightDownIndex]);
                            possibleList.Add(squareList[oppositeEdgeIndex]);
                        }
                        else if(plyrY == 0)
                        {
                            int leftDownIndex = 0;
                            int rightDownIndex = map - 1;
                            int oppositeEdgeIndex = map * (map - 1)+ plyrX;
                            possibleList.Add(squareList[leftDownIndex]);
                            possibleList.Add(squareList[rightDownIndex]);
                            possibleList.Add(squareList[oppositeEdgeIndex]);
                        }
                        else if(plyrY == map - 1)
                        {
                            int leftUpIndex = map * (map - 1);
                            int rightUpIndex = (map + 1)* (map - 1);
                            int oppositeEdgeIndex = plyrX;
                            possibleList.Add(squareList[leftUpIndex]);
                            possibleList.Add(squareList[rightUpIndex]);
                            possibleList.Add(squareList[oppositeEdgeIndex]);
                        }
                        if(possibleList.Count != 0) return GetRandomSquare(possibleList);
                    }

                }
                else
                {
                    List<square> cornerList = GetCornerSquares();
                    if(cornerList.Count != 0) 
                    {
                        hasAdvantage = true;
                        return GetRandomSquare(cornerList);
                    }
                }   
            }
            return GetRandomSquare();
        }
        else if(roundCount == 1)
        {
            if(canAdvantageAtStart && UnityEngine.Random.Range(0, 100) < difficult)
            {
                if(!isPlayerFirst && hasAdvantage)
                {
                    int comX = lastComputerPiece.ID.x;
                    int comY = lastComputerPiece.ID.y;
                    int plyrX = lastPlayerPiece.ID.x;
                    int plyrY = lastPlayerPiece.ID.y;
                    print("Last Player ID: " + lastPlayerPiece.ID);

                    if(plyrX == plyrY && plyrX == map - 1 - plyrY)
                    {
                        int oppositeCornerX = map - 1 - comX;
                        int oppositeCornerY = map - 1 - comY;
                        int oppositeCornerIndex = oppositeCornerX + map * oppositeCornerY;
                        return squareList[oppositeCornerIndex];
                    }
                    else if(plyrX == plyrY || plyrX == map - 1 - plyrY)
                    {
                        List<square> cornerList = GetCornerSquares();
                        cornerList.Remove(lastComputerPiece);
                        cornerList.Remove(lastPlayerPiece);
                        if(cornerList.Count != 0) return GetRandomSquare(cornerList);
                        else return CheckWeight(2);
                    }
                    else
                    {
                        int mid = (map - 1) / 2;
                        int centerIndex = (map + 1) * mid;
                        return squareList[centerIndex];
                    }
                }
                else return CheckWeight(2);
            }
            else return CheckWeight(2);
        }
        else
        {
            square square = CheckWeight(2);

            if(square.Weight == infinite) return square;
            else if(UnityEngine.Random.Range(0, 100) < difficult)
            {
                square defendSquare = CheckWeight(1);
                if(defendSquare.Weight == infinite) return defendSquare;
            }

            if(square is not null) return square;
            else return null; 
        } 
    }

    private square CheckWeight(int sourceState)
    {
        int enemyState = sourceState == 2 ? 1 : 2;
        foreach(square s in squareList) s.Weight = -1;

        for(int y = 0; y < map; y++)
        {
            
            int rowWeight = 0;
            for(int x = 0; x < map; x++)
            {
                int index = x + map * y;
                if(squareList[index].State == enemyState)
                {
                    rowWeight = -1;
                    break;
                }
                else if(squareList[index].State == sourceState)
                {
                    rowWeight++;
                }
            }
            for(int x = 0; x < map; x++)
            {
                int index = x + map * y;
                ChangeWeight(index, rowWeight);
            }
        }

        for(int x = 0; x < map; x++)
        {
            
            int columnWeight = 0;
            for(int y = 0; y < map; y++)
            {
                int index = x + map * y;
                if(squareList[index].State == enemyState)
                {
                    columnWeight = -1;
                    break;
                }
                else if(squareList[index].State == sourceState)
                {
                    columnWeight++;
                }
            }
            for(int y = 0; y < map; y++)
            {
                int index = x + map * y;
                ChangeWeight(index, columnWeight);
            }
        }

        int diagonalUpWeight = 0;
        for(int x = 0; x < map; x++)
        {
            int index = (map + 1) * x;
            if(squareList[index].State == enemyState)
            {
                diagonalUpWeight = -1;
                break;
            }
            else if(squareList[index].State == sourceState)
            {
                diagonalUpWeight++;
            }
        }
        for(int x = 0; x < map; x++)
        {
            int index = (map + 1) * x;
            ChangeWeight(index, diagonalUpWeight);
        }

        int diagonalDownWeight = 0;
        for(int x = 0; x < map; x++)
        {
            int index = (map - 1) * (map - x);
            if(squareList[index].State == enemyState)
            {
                diagonalDownWeight = -1;
                break;
            }
            else if(squareList[index].State == sourceState)
            {
                diagonalDownWeight++;
            }
        }
        for(int x = 0; x < map; x++)
        {
            int index = (map - 1) * (map - x);
            ChangeWeight(index, diagonalDownWeight);
        }

        List<square> blankList = squareList.FindAll(s => s.State == 0);
        int maxWeight = blankList.Max(s => s.Weight);
        List<square> maxWeightSquareList = new List<square>();
        foreach(square s in blankList)
        {
            if(s.Weight == maxWeight) maxWeightSquareList.Add(s);
        }

        return GetRandomSquare(maxWeightSquareList);
    }

    private void ChangeWeight(int index, int weight)
    {
        if(weight == -1) return;
        if(weight == map - 1 || squareList[index].Weight == infinite) 
        {
            squareList[index].Weight = infinite;
            return;
        }
        if(squareList[index].Weight == -1) squareList[index].Weight = weight;
        else squareList[index].Weight += weight; 
    }

    private square GetRandomSquare()
    {
        List<square> blankList = squareList.FindAll(s=>s.State == 0);
        if(blankList.Count == 0) return null;
        int rand = UnityEngine.Random.Range(0, blankList.Count);
        square s = blankList[rand];
        return s;
    }

    private square GetRandomSquare(List<square> blankList)
    {
        if(blankList.Count == 0) return null;
        int rand = UnityEngine.Random.Range(0, blankList.Count);
        square s = blankList[rand];
        return s;
    } 

    private List<square> GetCornerSquares()
    {
        List<square> cornerList = new List<square>();
        for(int a = 0; a < map; a++)
        {
            if(canAdvantageAtStart && a == (map - 1) / 2) continue;
            int index = (map + 1) * a;
            cornerList.Add(squareList[index]);
            index = (map - 1) * (map - a);
            cornerList.Add(squareList[index]);
        }
        return cornerList;
    }

    private bool CheckWin(square square)
    {
        for(int x = 0; x < map; x++)
        {
            int index = x + map * square.ID.y;
            if(squareList[index].State != square.State) break; 
            if(x == map - 1) return true;
        }

        for(int y = 0; y < map; y++)
        {
            int index = square.ID.x + map * y;
            if(squareList[index].State != square.State) break; 
            if(y == map - 1) return true;
        }

        if(square.ID.x == square.ID.y)
        {
            for(int x = 0; x < map; x++)
            {
                int index = (map + 1) * x;
                if(squareList[index].State != square.State) break; 
                if(x == map - 1) return true;
            }
        }

        if(square.ID.x == map - 1 - square.ID.y)
        {
            for(int x = 0; x < map; x++)
            {
                int index = (map - 1) * (map - x);
                if(squareList[index].State != square.State) break; 
                if(x == map - 1) return true;
            }
            return false;
        }
        else return false;
    }

    private bool CheckDraw(square square)
    {
        List<square> blankList = squareList.FindAll(s=>s.State == 0);
        if(blankList.Count == 0 && winState == -1) return true;
        else return false;
    }

    #region UI
    public void RestartScene()
    {
        StartCoroutine(WaitForRestart());
    }

    IEnumerator WaitForRestart()
    {
        yield return new WaitForSecondsRealtime(0.05f);
        SceneManager.LoadScene(0);
    }

    public void ChangeMap(int count)
    {
        map = count;
        RestartScene();
    }

    public void ChangeDifficult(InputField input)
    {
        if(int.TryParse(input.text, out difficult))
        {
            if(difficult > 100)
            {
                difficult = 100;
                input.text = difficult.ToString();
                print("Over 100: " + difficult);
            }
            else if (difficult < 0)
            {
                difficult = 0;
                input.text = difficult.ToString();
                print("Lower 0:" + difficult);
            }
            else input.text = difficult.ToString();
        }
        
    }
    public void PlaySoundAtAction()
    {
        Vector3 pos = cam.ScreenToWorldPoint(Input.mousePosition);
        AudioSource.PlayClipAtPoint(clickSound, pos, 0.5f);
    }
    #endregion
}
