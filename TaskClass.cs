using System;
using System.Collections.Generic;
using System.Text;

namespace StepperMotorController
{
    public class task
    {
        public bool motor1_enabled;
        public bool motor2_enabled;
        public bool motor3_enabled;

        public bool motor1_direction;
        public bool motor2_direction;
        public bool motor3_direction;

        public long motor1_period;
        public long motor2_period;
        public long motor3_period;

        public long motor1_steps;
        public long motor2_steps;
        public long motor3_steps;

        public bool isTimer;
        public long mSec;

        public task()
        {
            motor1_enabled = false;
            motor2_enabled = false;
            motor3_enabled = false;

            motor1_direction = false;
            motor2_direction = false;
            motor3_direction = false;

            motor1_period = 1000;
            motor2_period = 1000;
            motor3_period = 1000;

            motor1_steps = 200;
            motor2_steps = 200;
            motor3_steps = 200;

            isTimer = false;
            mSec = 0;
        }

    }
}
