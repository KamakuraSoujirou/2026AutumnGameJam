using System.Collections.Generic;
using UnityEngine;

// public class OrderGimmick : Gimmick
//{
    /*
    private enum Type{
        Number,
        Time,
        Alphabet,
        Size
    }
    Type _type;
    private int _clearCount = 0;
    // 数字を4つランダムに生成
    private int _upNumber;
    private int _rightNumber;
    private int _downNumber;
    private int _leftNumber;
    private List<InputDirection> _directionSet;
    protected bool _isReverse = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public string QuestionText()
    {
        return "";
    }
    public InputDirection NextAnswer()
    {

    }
    public void ResetRound()
    {

    }
    public void CreatePuzzle(int stage)
    {

    }
    public AnswerResult Answer(InputDirection direction)
    {
        if (direction == _directionSet[_clearCount])
        {
            _clearCount += 1;
            if(_clearCount == 4)
            {
                return AnswerResult.Correct;
            }
            else
            {
                return AnswerResult.Continue;
            }
        }
    }
    public void CreatePuzzle(int stage)
    {
        switch (stage)
        {
            case 1:
                _type = Type.Number;
                List<int> numberSet;
                _upNumber = Random.Range(1, 100);
                _rightNumber = Random.Range(1, 100);
                _downNumber = Random.Range(1, 100);
                _leftNumber = Random.Range(1, 100);
                numberSet = new List<int> { _upNumber, _rightNumber, _downNumber, _leftNumber };
                numberSet.Sort();
                for(int i = 0; i < 4; i++)
                {
                    if(numberSet[i] == _upNumber)
                    {
                        _directionSet.Add(InputDirection.Up);
                    }
                    else if (numberSet[i] == _rightNumber)
                    {
                        _directionSet.Add(InputDirection.Right);
                    }
                    else if (numberSet[i] == _downNumber)
                    {
                        _directionSet.Add(InputDirection.Down);
                    }
                    else if (numberSet[i] == _leftNumber)
                    {
                        _directionSet.Add(InputDirection.Left);
                    }
                }
                break;
            case 2:
                // C1,C3から一つランダムで選ぶ
                int randomNum = Random.Range(1, 3);
                if(randomNum == 1)
                {
                    _type = Type.Number;
                }
                else if(randomNum == 2)
                {
                    _type = Type.Time;
                }
                break;
            case 3:
                // C2,C3,C4から一つランダムで選ぶ
                int randomNum = Random.Range(1, 4);
                if (randomNum == 1)
                {
                    _type = Type.Alphabet;
                }
                else if (randomNum == 2)
                {
                    _type = Type.Time;
                }
                else if(randomNum == 3)
                {
                    _type = Type.Size;
                }
                break;
            case 4:
                // C2,C4,C5から一つランダムで選ぶ
                int randomNum = Random.Range(1, 4);
                if (randomNum == 1)
                {
                    _type = Type.Alphabet;
                }
                else if (randomNum == 2)
                {
                    _type = Type.Size;
                }
                else if (randomNum == 3)
                {
                    int rand = Random.Range(1, 5);
                    if (randomNum == 1)
                    {
                        _type = Type.Size;
                    }
                    else if (randomNum == 2)
                    {
                        _type = Type.Alphabet;
                    }
                    else if (randomNum == 3)
                    {
                        _type = Type.Time;
                    }
                    else if (randomNum == 4)
                    {
                        _type = Type.Size;
                    }
                }
                _isReverse = true;
                break;
        }
    }
    public void ResetRound()
    {
        _clearCount = 0;
    }
    */
//}
