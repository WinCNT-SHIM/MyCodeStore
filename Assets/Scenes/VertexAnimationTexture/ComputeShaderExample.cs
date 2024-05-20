using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderExample : MonoBehaviour
{
    // Start is called before the first frame update
    public ComputeShader computeShader;

    void Start()
    {
        // 입력 데이터를 생성합니다.
        int[] inputData = new int[10];
        for (int i = 0; i < inputData.Length; i++)
        {
            inputData[i] = i + 1;
        }

        // 입력 데이터 버퍼를 생성합니다.
        ComputeBuffer inputBuffer = new ComputeBuffer(inputData.Length, sizeof(int));
        inputBuffer.SetData(inputData);

        // 출력 데이터 버퍼를 생성합니다.
        ComputeBuffer outputBuffer = new ComputeBuffer(inputData.Length, sizeof(int));

        // 컴퓨팅 셰이더에 버퍼를 설정합니다.
        computeShader.SetBuffer(0, "inputBuffer", inputBuffer);
        computeShader.SetBuffer(0, "outputBuffer", outputBuffer);

        // 컴퓨팅 셰이더를 실행합니다.
        computeShader.Dispatch(0, inputData.Length, 1, 1);

        // 출력 데이터를 읽습니다.
        int[] outputData = new int[inputData.Length];
        outputBuffer.GetData(outputData);

        // 결과를 출력합니다.
        for (int i = 0; i < outputData.Length; i++)
        {
            Debug.Log($"Input: {inputData[i]}, Output: {outputData[i]}");
        }

        // 버퍼를 해제합니다.
        inputBuffer.Release();
        outputBuffer.Release();
    }
}
