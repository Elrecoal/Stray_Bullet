using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Elrecoal.Stray_Bullet
{

    [CreateAssetMenu(fileName = "New Gun", menuName = "Gun")]

    public class Gun : ScriptableObject
    {

        public string name;

        public int damage;

        public float bloom;

        public float reload;

        public int ammo;

        public int clipSize;

        public float recoil;

        public float kickback;

        public float rateOfFire;

        public float aimSpeed;

        public GameObject prefab;

        public int burst = 0; // 0 semi | 1 auto | 2+ r�faga

        private int stash; //Current ammo

        private int clip; //Current clip

        public void Init()
        {

            stash = ammo;

            clip = clipSize;

        }

        public bool FireBullet()
        {

            if (clip > 0)
            {

                clip -= 1;

                return true;

            }

            else
            {

                return false;

            }

        }

        public void Reload()
        {

            stash += clip;

            clip = Mathf.Min(clipSize, stash);

            stash -= clip;

        }

        public int GetStash()
        {
            return stash;
        }

        public int GetClip()
        {
            return clip;
        }

    }

}
