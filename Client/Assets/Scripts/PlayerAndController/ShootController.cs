using System;
using DG.Tweening;
using UnityEngine;

public class ShootController : LetterController<GWSPlayer>
{
    public event Action<GameObject, char> LetterShot;

    protected override void Start()
    {
        base.Start();

        LetterShot += (o, c) =>
        {
            if (c is ' ') return;
            DestroyLetter(o);
        };

        Player.DoingLetter += l =>
        {
            if (l is ' ') return;
            Player.currentLetter.GetComponent<Renderer>().material = Config.WordHighlightMat;
        };

        //I moved the bullet shooting to the controller temporarily, this is not a finale decision, the refactoring is not done
        Player.LetterDone += c =>
        {
            var letter = Player.currentLetter;

            if (c is ' ')
            {
                LetterShot?.Invoke(letter, c);
                return;
            }

            var bullet = BulletPool.I.Take(Player.gunMuzzle.transform.position, Quaternion.identity);
            var target = letter.transform.position;
            var distance = Vector3.Distance(transform.position, target);

            bullet.transform.DOMove(target, distance / Player.shootConfig.bulletSpeed)
                .SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                    LetterShot?.Invoke(letter, c);
                    BulletPool.I.Release(bullet);
                });
        };

        Player.PowerSkipping += w =>
        {
            foreach (var letter in GraphEnv.I.GetWordObjects(w, -1))
                DestroyLetter(letter);
        };
    }

    private void DestroyLetter(GameObject letter)
    {
        letter.transform.DOScale(0f, .15f);
        var parent = letter.transform.parent.gameObject;
        parent.GetComponentInChildren<ParticleSystem>().Play();
        // Destroy(parent);
    }
}