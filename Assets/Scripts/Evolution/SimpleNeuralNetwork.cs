using System.Collections.Generic;
using UnityEngine;

namespace Verrarium.Evolution
{
    /// <summary>
    /// Mạng nơ-ron đơn giản tạm thời - sẽ được thay thế bằng rtNEAT đầy đủ
    /// Cấu trúc tối thiểu: tất cả đầu vào kết nối trực tiếp với tất cả đầu ra
    /// </summary>
    public class SimpleNeuralNetwork
    {
        private int inputCount;
        private int outputCount;
        private float[,] weights; // weights[outputIndex, inputIndex]

        public SimpleNeuralNetwork(int inputs, int outputs)
        {
            inputCount = inputs;
            outputCount = outputs;
            weights = new float[outputs, inputs];

            // Khởi tạo trọng số ngẫu nhiên nhỏ
            for (int o = 0; o < outputs; o++)
            {
                for (int i = 0; i < inputs; i++)
                {
                    weights[o, i] = Random.Range(-1f, 1f);
                }
            }
        }

        /// <summary>
        /// Sao chép mạng nơ-ron
        /// </summary>
        public SimpleNeuralNetwork(SimpleNeuralNetwork parent)
        {
            inputCount = parent.inputCount;
            outputCount = parent.outputCount;
            weights = (float[,])parent.weights.Clone();
        }

        /// <summary>
        /// Tính toán đầu ra từ đầu vào
        /// </summary>
        public float[] Compute(float[] inputs)
        {
            if (inputs.Length != inputCount)
            {
                Debug.LogError($"Input count mismatch: expected {inputCount}, got {inputs.Length}");
                return new float[outputCount];
            }

            float[] outputs = new float[outputCount];

            for (int o = 0; o < outputCount; o++)
            {
                float sum = 0f;
                for (int i = 0; i < inputCount; i++)
                {
                    sum += inputs[i] * weights[o, i];
                }
                // Sigmoid activation
                outputs[o] = Sigmoid(sum);
            }

            return outputs;
        }

        /// <summary>
        /// Hàm kích hoạt Sigmoid
        /// </summary>
        private float Sigmoid(float x)
        {
            return 1f / (1f + Mathf.Exp(-x));
        }

        /// <summary>
        /// Đột biến trọng số
        /// </summary>
        public void Mutate(float mutationRate, float mutationStrength)
        {
            for (int o = 0; o < outputCount; o++)
            {
                for (int i = 0; i < inputCount; i++)
                {
                    if (Random.value < mutationRate)
                    {
                        weights[o, i] += Random.Range(-mutationStrength, mutationStrength);
                        weights[o, i] = Mathf.Clamp(weights[o, i], -5f, 5f);
                    }
                }
            }
        }

        /// <summary>
        /// Thay đổi một trọng số ngẫu nhiên
        /// </summary>
        public void ChangeRandomWeight(float amount)
        {
            int o = Random.Range(0, outputCount);
            int i = Random.Range(0, inputCount);
            weights[o, i] = Mathf.Clamp(weights[o, i] + amount, -5f, 5f);
        }

        /// <summary>
        /// Đảo ngược dấu của một trọng số ngẫu nhiên
        /// </summary>
        public void FlipRandomWeight()
        {
            int o = Random.Range(0, outputCount);
            int i = Random.Range(0, inputCount);
            weights[o, i] = -weights[o, i];
        }
    }
}

