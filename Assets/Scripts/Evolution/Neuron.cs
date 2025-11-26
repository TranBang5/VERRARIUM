using UnityEngine;

namespace Verrarium.Evolution
{
    /// <summary>
    /// Đại diện cho một nơ-ron trong mạng NEAT
    /// </summary>
    [System.Serializable]
    public class Neuron
    {
        public int id; // ID duy nhất của nơ-ron
        public NeuronType type; // Loại nơ-ron (Input, Hidden, Output)
        public ActivationFunction activationFunction; // Hàm kích hoạt
        public float bias; // Bias của nơ-ron
        public float value; // Giá trị hiện tại (sau khi tính toán)

        public Neuron(int id, NeuronType type, ActivationFunction activation = ActivationFunction.Sigmoid)
        {
            this.id = id;
            this.type = type;
            this.activationFunction = activation;
            this.bias = 0f;
            this.value = 0f;
        }

        /// <summary>
        /// Sao chép nơ-ron
        /// </summary>
        public Neuron(Neuron other)
        {
            this.id = other.id;
            this.type = other.type;
            this.activationFunction = other.activationFunction;
            this.bias = other.bias;
            this.value = 0f; // Reset giá trị
        }

        /// <summary>
        /// Áp dụng hàm kích hoạt
        /// </summary>
        public float Activate(float input)
        {
            float activated = input + bias;
            
            switch (activationFunction)
            {
                case ActivationFunction.Sigmoid:
                    return 1f / (1f + Mathf.Exp(-activated));
                case ActivationFunction.Tanh:
                    return (float)System.Math.Tanh(activated);
                case ActivationFunction.ReLU:
                    return Mathf.Max(0f, activated);
                case ActivationFunction.Linear:
                    return activated;
                default:
                    return 1f / (1f + Mathf.Exp(-activated));
            }
        }
    }

    public enum NeuronType
    {
        Input,
        Hidden,
        Output
    }

    public enum ActivationFunction
    {
        Sigmoid,
        Tanh,
        ReLU,
        Linear
    }
}

