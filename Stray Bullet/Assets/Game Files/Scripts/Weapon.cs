using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;
using TMPro;


namespace Com.Elrecoal.Stray_Bullet
{

    public class Weapon : MonoBehaviourPunCallbacks
    {


        public Gun[] loadout;
        [HideInInspector] public Gun currentGunData;

        public Transform weaponParent;

        public GameObject bulletHolePrefab;
        public LayerMask canBeShot;

        public bool isAiming = false;

        private GameObject currentWeapon;
        private int currentIndex;

        private float currentCooldown = 0;


        private bool isReloading;

        public AudioSource sfx;

        private void Start()
        {
            foreach (Gun g in loadout) g.Init();

            Equip(0);

        }

        void Update()
        {

            if (Pause.paused && photonView.IsMine) return;

            if (photonView.IsMine)
            {
                if (Input.GetKey(KeyCode.Alpha1) && currentIndex != 0 && !isReloading) photonView.RPC("Equip", RpcTarget.All, 0);
                if (Input.GetKey(KeyCode.Alpha2) && currentIndex != 1 && !isReloading) photonView.RPC("Equip", RpcTarget.All, 1);
            }

            if (currentWeapon != null)
            {

                if (photonView.IsMine)
                {

                    Aim(Input.GetMouseButton(1));

                    if (loadout[currentIndex].burst != 1)
                    {
                        if (Input.GetMouseButtonDown(0) && currentCooldown <= 0 && !isReloading)
                        {
                            if (loadout[currentIndex].FireBullet()) photonView.RPC("Shoot", RpcTarget.All);
                            else if (loadout[currentIndex].GetStash() > 0) StartCoroutine(Reload(loadout[currentIndex].reload));
                        }
                    }
                    else
                    {
                        if (Input.GetMouseButton(0) && currentCooldown <= 0 && !isReloading)
                        {
                            if (loadout[currentIndex].FireBullet()) photonView.RPC("Shoot", RpcTarget.All);
                            else if (loadout[currentIndex].GetStash() > 0) StartCoroutine(Reload(loadout[currentIndex].reload));
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.R) && !isReloading && loadout[currentIndex].GetStash() > 0 && loadout[currentIndex].GetClip() < loadout[currentIndex].clipSize) StartCoroutine(Reload(loadout[currentIndex].reload));

                    //Cooldown
                    if (currentCooldown > 0) currentCooldown -= Time.deltaTime;

                }

                //Weapon position elasticity
                currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);

            }

        }


        public void RefreshAmmo(TMP_Text p_text)
        {

            int t_clip = loadout[currentIndex].GetClip();

            int t_stash = loadout[currentIndex].GetStash();

            p_text.text = t_clip.ToString("D2") + " / " + t_stash.ToString("D2");

        }

        IEnumerator Reload(float p_wait)
        {

            isReloading = true;

            currentWeapon.SetActive(false);

            yield return new WaitForSeconds(p_wait);

            loadout[currentIndex].Reload();

            currentWeapon.SetActive(true);

            isReloading = false;

        }

        [PunRPC]
        void Equip(int p_ind)
        {

            //-----------------------------------Usar rueda del rat�n para ciclar entre armas (tener en cuenta final de loadout y volver a empezar o poner el ultimo arma de limite?)-----------------------------------
            if (p_ind < loadout.Length)
            {

                if (currentWeapon != null)
                {

                    StopCoroutine("Reload");

                    Destroy(currentWeapon);

                }

                currentIndex = p_ind;

                GameObject t_newEquipment = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;

                t_newEquipment.transform.localPosition = Vector3.zero;

                t_newEquipment.transform.localEulerAngles = Vector3.zero;

                t_newEquipment.GetComponent<Sway>().isMine = photonView.IsMine;

                currentWeapon = t_newEquipment;
                currentGunData = loadout[p_ind];
            }

        }

        void Aim(bool p_isAiming)
        {

            isAiming = p_isAiming;

            Transform t_anchor = currentWeapon.transform.Find("Anchor");

            Transform t_state_ads = currentWeapon.transform.Find("States/ADS");

            Transform t_state_hip = currentWeapon.transform.Find("States/Hip");

            if (p_isAiming) t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * loadout[currentIndex].aimSpeed);

            else t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);

        }

        [PunRPC]
        void Shoot()
        {

            Transform t_spawn = transform.Find("Cameras/Normal Camera");

            //Cooldown (segundos que tarda en poder volver a disparar)
            currentCooldown = loadout[currentIndex].rateOfFire;

            for (int i = 0; i < Mathf.Max(1,currentGunData.pellets); i++)
            {
                //Bloom
                Vector3 t_bloom = t_spawn.position + t_spawn.forward * 1000f;
                t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.up;
                t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.right;
                t_bloom -= t_spawn.position;
                t_bloom.Normalize();

                //Raycast
                RaycastHit t_hit = new RaycastHit();

                if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, 1000f, canBeShot))
                {
                    //-----------------------------------Modificar si a�ado explosivos para que sean diferentes agujeros de bala/explosivo-----------------------------------
                    //-----------------------------------Modificar para que las balas no se pongan en la cara de los jugadores y solucionar el que apunte siempre hacia delante-----------------------------------
                    GameObject t_newBulletHole = Instantiate(bulletHolePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;

                    t_newBulletHole.transform.LookAt(t_hit.point + t_hit.normal);

                    Destroy(t_newBulletHole, 5);

                    if (photonView.IsMine)
                    {

                        //Shooting a player
                        if (t_hit.collider.gameObject.layer == 11)
                        {

                            //RpcTarget call to damage player
                            t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage);

                        }

                    }

                }
            }

            //Sound
            sfx.Stop();
            sfx.clip = currentGunData.gunShotSound;
            sfx.pitch = 1 - currentGunData.pitchRandomization + Random.Range(-currentGunData.pitchRandomization, currentGunData.pitchRandomization);
            sfx.volume = currentGunData.gunShotVolume;
            sfx.Play();

            //Gun effect
            currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0, 0);
            currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickback;
            //if (currentGunData.recovery) currentWeapon.GetComponent<Animator>().Play("Recovery", 0, 0); Añadir cuando añada animaciones de "recuperacion"
        }

        [PunRPC]
        void TakeDamage(int p_damage)
        {

            GetComponent<Player>().TakeDamage(p_damage);

        }


    }

}