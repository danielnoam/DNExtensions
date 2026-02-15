using System.Collections;
using DNExtensions.Systems.ObjectPooling;
using UnityEngine;

public class ObjectPoolerTester : MonoBehaviour
{
    [SerializeField] private float bulletLifeTime = 5f;
    [SerializeField] private float rateOfFire = 1;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private GameObject bulletPrefab;

    private bool _isShooting;
    
    private void Start()
    {
        StartCoroutine(SpawnBulletsRoutine());
    }

    private IEnumerator SpawnBulletsRoutine()
    {
        _isShooting = true;
        
        while (_isShooting)
        {
            SpawnBullet();
            yield return new WaitForSeconds(1f / rateOfFire);
        }
        
        yield break;
    }

    private void SpawnBullet()
    {
        var bulletGo = ObjectPooler.GetObjectFromPool(bulletPrefab);
        StartCoroutine(BulletLifetimeRoutine(bulletGo));
    }

    private IEnumerator BulletLifetimeRoutine(GameObject bullet)
    {
        if (!bullet) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < bulletLifeTime)
        {
            if (!bullet) yield break;
            
            bullet.transform.position += bullet.transform.forward * (bulletSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (bullet)
        {
            ObjectPooler.ReturnObjectToPool(bullet);
        }

    }
}