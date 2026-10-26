using UnityEngine;

// Script ini dipasang otomatis oleh GameManager ke setiap kubus jawaban.
// Kamu tidak perlu memasangnya manual.
public class AnswerCube : MonoBehaviour
{
    public int value;
    public bool isCorrect;
    public GameManager manager;

    void OnTriggerEnter(Collider other)
    {
        // Hanya mobil milik GameManager ini yang boleh menjawab (penting di mode DUO)
        var car = other.GetComponent<PlayerCar>();
        if (car != null && manager != null && car == manager.player)
            manager.OnAnswerHit(this);
    }
}
