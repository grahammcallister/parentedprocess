using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ParentProcess.Test
{
    [TestFixture]
    public class ParentedProcessTestFixture
    {
        [TestCase]
        public void ParentProcessManagerConstructor_WithNullFilename_ThrowsArgumentNullException()
        {
            // Arrange + Act + Assert
            Assert.Throws<ArgumentNullException>(() => { new ProcessManager(null, null, null); });
        }

        [TestCase]
        public void ParentProcessManagerConstructor_WithEmptyFilename_ThrowsArgumentNullException()
        {
            // Arrange + Act + Assert
            Assert.Throws<ArgumentNullException>(() => { new ProcessManager(string.Empty, null, null); });
        }

        [TestCase]
        public void ParentProcessManagerConstructor_WithNotFoundFilename_ThrowsFileNotFoundException()
        {
            // Arrange + Act + Assert
            Assert.Throws<FileNotFoundException>(() => { new ProcessManager(@"C:\Foo\Bar.exe", null, null); });
        }

        [TestCase]
        public void ParentProcessManager_WithFaultingApplication_FiresUnhandledExceptionEvent()
        {
            var path = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var finalPath = Path.Combine(path, @"..\..\..\ParentProcess.Test.FaultingAppTest\bin\Debug\ParentProcess.Test.FaultingAppTest.exe");
            // Arrange
            var manager = new ProcessManager(finalPath, "Faulting process", "Faulting process");
            manager.ProcessUnhandledExceptionEvent += Manager_ProcessUnhandledExceptionEvent;

            // Act
            manager.StartProcess();

            Thread.Sleep(6000);

            // Assert

            Assert.That(_unhandledException, Is.Not.Null);
            Assert.That(manager.HasExited, Is.True);
        }

        [TestCase]
        public void ParentProcessManager_WithNonResponsiveApplication_FiresNonResponsiveEvent()
        {
            // Arrange
            _nonResponsive = false;
            var path = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var finalPath = Path.Combine(path, @"..\..\..\ParentProcess.Test.NonResponsiveAppTest\bin\Debug\ParentProcess.Test.NonResponsiveAppTest.exe");
            
            var manager = new ProcessManager(finalPath, "NonResponsive process", "NonResponsive process");
            manager.ProcessNonResponsiveEvent += Manager_ProcessNonResponsiveEvent;

            // Act
            manager.StartProcess();

            Thread.Sleep(15000);

            manager.StopProcess();

            Thread.Sleep(2000);

            // Assert

            Assert.That(_nonResponsive, Is.True);
        }

        [TestCase]
        public void ParentProcessManager_WithSlowClosingApplication_ClosesApplication()
        {
            // Arrange
            _wasStopped = false;
            var path = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var finalPath = Path.Combine(path, @"..\..\..\ParentProcess.Test.SlowClosingAppTest\bin\Debug\ParentProcess.Test.SlowClosingAppTest.exe");

            var manager = new ProcessManager(finalPath, "Slow closing process", "Slow closing process");
            manager.ProcessStoppedEvent += Manager_ProcessStoppedEvent;

            // Act
            manager.StartProcess();

            Thread.Sleep(1000);

            manager.StopProcess();

            while(!manager.ParentedProcessInfo.Process.HasExited)
            {
                Task.Delay(1000);
            }

            Thread.Sleep(2000);

            // Assert
            Assert.That(_wasStopped, Is.True);
        }

        private void Manager_ProcessNonResponsiveEvent(EventArgs args)
        {
            _nonResponsive = true;
        }

        [TestCase]
        public void ParentProcessManager_WhenStoppingProcess_FiresStoppedEventAndProcessIsNoLongerRunning()
        {
            // Arrange
            _wasStopped = false;
            _unhandledException = null;
            var path = @"C:\Windows\System32\Notepad.exe";
            var manager = new ProcessManager(path, "Notepad will be killed", "Notepad will be killed");
            manager.ProcessStoppedEvent += Manager_ProcessStoppedEvent;
            manager.ProcessUnhandledExceptionEvent += Manager_ProcessUnhandledExceptionEvent;

            // Act
            manager.StartProcess();

            Thread.Sleep(1000);

            manager.StopProcess();

            Thread.Sleep(5000);

            // Assert

            Assert.That(_wasStopped, Is.True);
            Assert.That(manager.HasExited, Is.True);
            Assert.That(manager.ParentedProcessInfo.Process.HasExited, Is.True);
            Assert.That(_unhandledException, Is.Null);
        }

        private void Manager_ProcessStoppedEvent(EventArgs args)
        {
            _wasStopped = true;
        }

        private void Manager_ProcessUnhandledExceptionEvent(object sender, UnhandledExceptionEventArgs args)
        {
            _unhandledException = args.ExceptionObject as Exception;
        }

        [TestCase]
        public void ParentProcessManager_StartProcessWithRunningApplication_HasExitedFalse()
        {
            // Arrange
            var path = @"C:\Windows\System32\Notepad.exe";
            var manager = new ProcessManager(path, "Notepad parented", "Notepad parented");

            // Act
            manager.StartProcess();

            Thread.Sleep(2000);

            // Assert
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.HasExited, Is.False);

            // Clean up
            manager.StopProcess();
        }

        private bool _wasStopped = false;
        private bool _nonResponsive = false;
        private Exception _unhandledException;
        private string _mainWindowHandle = string.Empty;
        private ProcessManager _manager;

        [TestCase]
        public void ParentProcessManager_StartProcessWithMainWindow_CanFindMainWindowHandle()
        {
            // Arrange
            var path = @"C:\Windows\System32\Notepad.exe";
            _manager = new ProcessManager(path, "Notepad parented", "Notepad parented");

            // Act
            _manager.StartProcess();
            _manager.ProcessMainWindowHandleFoundEvent += Manager_ProcessMainWindowHandleFoundEvent;
            Thread.Sleep(2000);
            _manager.FindMainWindowHandle();
            Thread.Sleep(2000);

            // Assert
            Assert.That(_manager, Is.Not.Null);
            Assert.That(_manager.HasExited, Is.False);
            Assert.That(_mainWindowHandle, Is.Not.Null);
            Assert.That(_mainWindowHandle, Is.Not.Empty);
            Assert.That(_mainWindowHandle, Is.Not.EqualTo("-1"));

            // Clean up
            _manager.ParentedProcessInfo.Process.Kill();
        }

        private void Manager_ProcessMainWindowHandleFoundEvent(EventArgs args)
        {
            _mainWindowHandle = _manager.ParentedProcessInfo.MainWindowHandle.ToString();
        }

        [TestCase]
        public void ParentProcessManager_WithApplicationKilled_FiresUnhandledExceptionEvent()
        {
            // Arrange
            _wasStopped = false;
            _unhandledException = null;
            var path = @"C:\Windows\System32\Notepad.exe";
            var manager = new ProcessManager(path, "Notepad", "Notepad");
            manager.ProcessUnhandledExceptionEvent += Manager_ProcessUnhandledExceptionEvent;
            manager.StartProcess();
            Thread.Sleep(3000);

            // Act
            manager.ParentedProcessInfo.Process.Kill();
            Thread.Sleep(7000);

            // Assert
            Assert.That(_unhandledException, Is.Not.Null);
            Assert.That(manager.HasExited, Is.True);
        }

    }
}