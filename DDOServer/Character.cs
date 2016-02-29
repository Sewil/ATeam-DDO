﻿using System;

namespace DDOServer {
    public abstract class Character {
        public string Name { get; set; }
        public int Damage { get; set; }
        int health;
        public int Health {
            get {
                return health;
            }
            set {
                health = value;
                if (health <= 0) {
                    OnDied();
                }
            }
        }
        public ConsoleColor Color { get; set; }
        public int Gold { get; set; }
        public event Action<Character> Died;
        public void OnDied() {
            Died?.Invoke(this);
        }
        public void DiedUnsubscribe() {
            Died = null;
        }
    }
}