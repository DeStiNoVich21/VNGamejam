using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DIALOGUE;

namespace TESTING
{
    public class Testing_aAa : MonoBehaviour
    {
        DialogueSystem ds;
        TextArchitect architect;

        public TextArchitect.BuildMethod bm = TextArchitect.BuildMethod.instant;

        string[] lines = new string[5]
        {
            "saassssssssssssd",
            "sdadaaaaaaaaaaaa",
            "sddddddddddddfdsfsdf",
            "dffdfgsdfgdrthdfthjhjdfg",
            "dsssssssssssssssssffffff"
        };

        private void Start()
        {
            ds = DialogueSystem.instance;
            architect = new TextArchitect(ds.dialogueContainer.dialogueText);
            architect.buildMethod = TextArchitect.BuildMethod.fade;
            architect.speed = 0.5f;
        }

        private void Update()
        {
            if(bm != architect.buildMethod)
            {
                architect.buildMethod = bm;
                architect.Stop();
            }

            if (Input.GetKeyDown(KeyCode.S))
                architect.Stop();
            string LONGLINE = "It's really long line that the make's no sense and it's for really long test mother fuker, we all like stuf, but i dont know how to continue this string, i cant imagine something, and so i think thats all";
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (architect.isBuilding)
                {
                    if (!architect.hurryUp)
                    {
                        architect.hurryUp = true;
                    }
                    else
                    {
                        architect.ForceComplete();
                    }
                }
                else
                {
                    architect.Build(LONGLINE);
                    //architect.Build(lines[Random.Range(0, 5)]);
                }
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                architect.Append(LONGLINE);
                //architect.Append(lines[Random.Range(0, 5)]);
            }
        }
    }
}