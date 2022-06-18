# Woof Man Kill

wolf man kill cli game

# Gameplay Design

town of salem features:

- last wish

- notes

- whisper

town role:

- bodyguard
  
  - protect one person each night
  
  - if you are attacked, you and the attacker die
  
  - if your protected target is attacked, your target will live
  
  - you will know if your target is attacked

- doctor
  
  - heal one person each night
  
  - you can only hear yourself once every two nights
  
  - you will know if your target is attacked

- sheriff
  
  - investigate 2 persons each night
  
  - you will know if the 2 persons are of same side

- swapper
  
  - swap the position of 2 persons each night
  
  - if the one of them was being taken an action, the other one is affected instead

- spy
  
  - check whether a person visited another person each night

woof role:

- killer
  
  - kill one person at night

- freezer
  
  - freeze a person's action at night

# Implementation Design

all data stored in server

client only responsible for user input and game output

major features:

- chat app function

menu:

- host game

- join game

lobby:

- see ip address

- see joined player

- enable role reveal

day time:

- whisper function (specialized chat app)

- vote system

- announcement function

night time:

- woof talk

- individual actions

death:

- last wish reveal

- role reveal
